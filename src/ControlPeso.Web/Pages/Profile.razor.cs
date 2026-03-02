using System.Security.Claims;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Pages;

/// <summary>
/// Profile page code-behind with DTO-first architecture.
/// Uses ProfileFormModel for clean state management and MudBlazor binding.
/// </summary>
public partial class Profile : IDisposable
{
    // ========================================================================
    // DEPENDENCY INJECTION
    // ========================================================================

    [Inject] private IStringLocalizer<Profile> Localizer { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private IUserPreferencesService UserPreferencesService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ILogger<Profile> Logger { get; set; } = null!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private Services.UserStateService UserStateService { get; set; } = null!;
    [Inject] private Services.ThemeService ThemeService { get; set; } = null!;
    [Inject] private IWebHostEnvironment WebHostEnvironment { get; set; } = null!;

    // ========================================================================
    // STATE
    // ========================================================================
    
    /// <summary>
    /// Single source of truth for form state - DTO pattern.
    /// All MudBlazor components bind directly to this model.
    /// </summary>
    private ProfileFormModel _formModel = ProfileFormModel.CreateDefault();
    
    /// <summary>
    /// Current user data from database (read-only reference).
    /// </summary>
    private UserDto? _user;
    
    /// <summary>
    /// Weight statistics for display in stats cards.
    /// </summary>
    private WeightStatsDto? _stats;
    
    /// <summary>
    /// Selected avatar file for upload.
    /// </summary>
    private IBrowserFile? _selectedFile;
    
    /// <summary>
    /// Reference to image cropper dialog.
    /// </summary>
    private IDialogReference? _cropperDialog;
    
    /// <summary>
    /// Cache busting version for avatar URL.
    /// </summary>
    private long _avatarVersion = DateTime.UtcNow.Ticks;
    
    /// <summary>
    /// Loading state for initial data fetch.
    /// </summary>
    private bool _isLoading = true;

    /// <summary>
    /// Saving state for save button.
    /// </summary>
    private bool _isSaving;

    /// <summary>
    /// Flag to prevent concurrent LoadUserDataAsync calls.
    /// </summary>
    private bool _isLoadingData;

    // ========================================================================
    // PUBLIC PROPERTIES FOR MUDBLAZOR BINDING
    // ========================================================================
    
    /// <summary>
    /// Form model exposed for MudBlazor @bind-Value.
    /// </summary>
    public ProfileFormModel FormModel => _formModel;

    // ========================================================================
    // LOCALIZED STRINGS
    // ========================================================================
    
    // Page & Meta
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];
    private string MetaKeywords => Localizer["MetaKeywords"];
    private string OgTitle => Localizer["OgTitle"];
    private string OgDescription => Localizer["OgDescription"];

    // Header Section
    private string ClickToChangePhotoTooltip => Localizer["ClickToChangePhotoTooltip"];
    private string SaveChangesButton => Localizer["SaveChangesButton"];
    private string SavingButton => Localizer["SavingButton"];

    // Stats Cards
    private string StartingWeightLabel => Localizer["StartingWeightLabel"];
    private string CurrentWeightLabel => Localizer["CurrentWeightLabel"];
    private string TotalLossLabel => Localizer["TotalLossLabel"];
    private string KgUnit => Localizer["KgUnit"];
    private string NoData => Localizer["NoData"];

    // Personal Details Card
    private string PersonalDetailsTitle => Localizer["PersonalDetailsTitle"];
    private string NameLabel => Localizer["NameLabel"];
    private string NameRequired => Localizer["NameRequired"];
    private string HeightLabel => Localizer["HeightLabel"];
    private string HeightRequired => Localizer["HeightRequired"];
    private string CmUnit => Localizer["CmUnit"];
    private string UnitSystemLabel => Localizer["UnitSystemLabel"];
    private string UnitSystemMetric => Localizer["UnitSystemMetric"];
    private string UnitSystemImperial => Localizer["UnitSystemImperial"];
    private string DateOfBirthLabel => Localizer["DateOfBirthLabel"];
    private string GoalWeightLabel => Localizer["GoalWeightLabel"];
    private string LanguageLabel => Localizer["LanguageLabel"];
    private string LanguageSpanish => Localizer["LanguageSpanish"];
    private string LanguageEnglish => Localizer["LanguageEnglish"];
    private string DarkModeLabel => Localizer["DarkModeLabel"];
    private string NotificationsLabel => Localizer["NotificationsLabel"];

    // Account Settings Card
    private string AccountSettingsTitle => Localizer["AccountSettingsTitle"];
    private string EmailAddressLabel => Localizer["EmailAddressLabel"];
    private string SignOutButton => Localizer["SignOutButton"];
    private string DeleteAccountButton => Localizer["DeleteAccountButton"];

    // Error Messages
    private string ErrorLoadingProfile => Localizer["ErrorLoadingProfile"];
    private string ErrorUserNotIdentified => Localizer["ErrorUserNotIdentified"];
    private string ErrorUserNotFound => Localizer["ErrorUserNotFound"];
    private string ErrorLoadingProfileGeneral => Localizer["ErrorLoadingProfileGeneral"];
    private string ErrorUpdatingProfile => Localizer["ErrorUpdatingProfile"];
    private string ErrorChangingTheme => Localizer["ErrorChangingTheme"];
    private string ErrorChangingNotificationPreference => Localizer["ErrorChangingNotificationPreference"];
    private string ErrorSigningOut => Localizer["ErrorSigningOut"];
    private string ErrorDeletingAccount => Localizer["ErrorDeletingAccount"];
    private string PhotoSizeTooLarge => Localizer["PhotoSizeTooLarge"];
    private string PhotoInvalidFormat => Localizer["PhotoInvalidFormat"];

    // Success Messages
    private string ProfileSavedSuccess => Localizer["ProfileSavedSuccess"];
    private string AccountDeletedSuccess => Localizer["AccountDeletedSuccess"];
    private string PhotoUploadedSuccess => Localizer["PhotoUploadedSuccess"];

    // Delete Account Dialog
    private string DeleteAccountConfirmation => Localizer["DeleteAccountConfirmation"];
    private string DeleteAccountButtonText => Localizer["DeleteAccountButtonText"];
    private string CropImageDialogTitle => Localizer["CropImageDialogTitle"];
    private string DeleteAccountDialogTitle => Localizer["DeleteAccountDialogTitle"];
    private string AccountDeletionNotImplemented => Localizer["AccountDeletionNotImplemented"];

    // Methods with placeholders
    private string GetMemberSinceText() => Localizer["MemberSince", _user?.MemberSince.ToString("MMMM yyyy") ?? string.Empty];
    private string GetErrorUploadingPhoto(string error) => Localizer["ErrorUploadingPhoto", error];

    // ========================================================================
    // LIFECYCLE METHODS
    // ========================================================================

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Profile: OnInitializedAsync - New circuit/F5 detected");

        // Subscribe to UserStateService events (only once per circuit)
        UserStateService.UserThemeUpdated += OnUserThemeUpdatedExternal;

        // FORCE load user data on F5/new circuit
        await LoadUserDataAsync();

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogInformation("Profile: OnParametersSetAsync - Navigation detected (SPA routing)");

        // FORCE load user data on every SPA navigation
        await LoadUserDataAsync();

        await base.OnParametersSetAsync();
    }

    /// <summary>
    /// FORCE loads user data from database on EVERY call (F5, navigation, etc).
    /// Ensures avatar URL is ALWAYS fresh from SQL Server.
    /// RACE CONDITION FIX: Prevents concurrent executions from OnInitializedAsync + OnParametersSetAsync.
    /// </summary>
    private async Task LoadUserDataAsync()
    {
        // CRITICAL: Prevent concurrent calls (race condition when F5 triggers both lifecycle methods)
        if (_isLoadingData)
        {
            Logger.LogDebug("Profile: LoadUserDataAsync already in progress - skipping duplicate call to prevent race condition");
            return;
        }

        _isLoadingData = true;
        Logger.LogInformation("Profile: LoadUserDataAsync - FORCE reload from DB started (lock acquired)");

        _isLoading = true;

        try
        {
            // 1. Get authenticated user ID
            var userId = await GetAuthenticatedUserIdAsync();
            if (userId is null)
            {
                _isLoading = false;
                return;
            }

            // 2. FORCE load user profile from database (ALWAYS fetch fresh)
            _user = await LoadUserProfileAsync(userId.Value);
            if (_user is null)
            {
                _isLoading = false;
                return;
            }

            // 3. Update cache busting version to FORCE browser reload avatar
            _avatarVersion = DateTime.UtcNow.Ticks;
            Logger.LogInformation("Profile: Avatar URL from DB: {AvatarUrl}, Cache buster: v={Version}", 
                _user.AvatarUrl ?? "(null)", _avatarVersion);

            // 4. Map UserDto → ProfileFormModel (DTO mapping)
            MapUserDtoToFormModel(_user);

            // 5. Load user preferences (Dark Mode, Notifications)
            await LoadUserPreferencesAsync(userId.Value);

            // 6. Load weight statistics
            await LoadWeightStatisticsAsync(userId.Value);

            Logger.LogInformation("Profile: ✅ User data loaded from DB - UserId: {UserId}, AvatarUrl: {AvatarUrl}", 
                userId, _user.AvatarUrl ?? "(null)");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: ❌ Error loading user data");
            Snackbar.Add(ErrorLoadingProfileGeneral, Severity.Error);
        }
        finally
        {
            // CRITICAL: Force re-render BEFORE releasing loading flags
            // This ensures component renders with fresh data, not stale cached values
            await InvokeAsync(StateHasChanged);

            _isLoading = false;
            _isLoadingData = false; // Release lock for next call
            Logger.LogDebug("Profile: LoadUserDataAsync - lock released");
        }
    }

    public void Dispose()
    {
        // Cleanup event subscriptions if needed
        UserStateService.UserThemeUpdated -= OnUserThemeUpdatedExternal;
    }

    // ========================================================================
    // PRIVATE HELPER METHODS - LOADING
    // ========================================================================
    
    /// <summary>
    /// Gets authenticated user ID from claims.
    /// </summary>
    private async Task<Guid?> GetAuthenticatedUserIdAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Logger.LogWarning("Profile: User ID claim not found or invalid");
            Snackbar.Add(ErrorUserNotIdentified, Severity.Error);
            return null;
        }

        return userId;
    }
    
    /// <summary>
    /// Loads user profile from database.
    /// </summary>
    private async Task<UserDto?> LoadUserProfileAsync(Guid userId)
    {
        Logger.LogDebug("Profile: Loading user profile - UserId: {UserId}", userId);
        
        var user = await UserService.GetByIdAsync(userId);
        
        if (user is null)
        {
            Logger.LogWarning("Profile: User not found in database - UserId: {UserId}", userId);
            Snackbar.Add(ErrorUserNotFound, Severity.Error);
            return null;
        }

        Logger.LogInformation(
            "Profile: User loaded from DB - Name: '{Name}', Height: {Height}cm, GoalWeight: {GoalWeight}kg, " +
            "UnitSystem: {UnitSystem}, Language: '{Language}'",
            user.Name, user.Height, user.GoalWeight, user.UnitSystem, user.Language);

        return user;
    }
    
    /// <summary>
    /// Maps UserDto to ProfileFormModel for form binding.
    /// </summary>
    private void MapUserDtoToFormModel(UserDto user)
    {
        _formModel = new ProfileFormModel
        {
            Name = user.Name,
            Height = user.Height,
            DateOfBirth = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
            GoalWeight = user.GoalWeight,
            UnitSystem = user.UnitSystem,
            Language = user.Language
        };

        Logger.LogInformation(
            "Profile: Form model populated - Name: '{Name}', Height: {Height}cm, GoalWeight: {GoalWeight}kg, " +
            "UnitSystem: {UnitSystem}",
            _formModel.Name, _formModel.Height, _formModel.GoalWeight, _formModel.UnitSystem);
    }
    
    /// <summary>
    /// Loads user preferences (Dark Mode, Notifications).
    /// </summary>
    private async Task LoadUserPreferencesAsync(Guid userId)
    {
        try
        {
            _formModel.DarkMode = await UserPreferencesService.GetDarkModePreferenceAsync(userId);
            _formModel.NotificationsEnabled = await UserPreferencesService.GetNotificationsEnabledAsync(userId);

            Logger.LogInformation(
                "Profile: Preferences loaded - DarkMode: {DarkMode}, Notifications: {Notifications}",
                _formModel.DarkMode, _formModel.NotificationsEnabled);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error loading preferences - UserId: {UserId}, using defaults", userId);
            // Continue with defaults already set in ProfileFormModel
        }
    }
    
    /// <summary>
    /// Loads weight statistics for stats cards.
    /// </summary>
    private async Task LoadWeightStatisticsAsync(Guid userId)
    {
        try
        {
            _stats = await WeightLogService.GetStatsAsync(
                userId,
                new Application.Filters.DateRange
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today)
                });

            Logger.LogDebug(
                "Profile: Weight stats loaded - Current: {Current}kg, Starting: {Starting}kg, Change: {Change}kg",
                _stats?.CurrentWeight, _stats?.StartingWeight, _stats?.TotalChange);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Profile: Could not load weight statistics - UserId: {UserId}", userId);
            // Continue without stats - not critical
        }
    }

    // ========================================================================
    // EVENT HANDLERS - PROFILE ACTIONS
    // ========================================================================
    
    /// <summary>
    /// Saves profile changes to database.
    /// </summary>
    private async Task SaveChanges()
    {
        if (_user is null)
            return;

        Logger.LogInformation("Profile: Saving changes - UserId: {UserId}", _user.Id);
        _isSaving = true;

        try
        {
            // 1. Map ProfileFormModel → UpdateUserProfileDto
            var updateDto = new UpdateUserProfileDto
            {
                Name = _formModel.Name.Trim(),
                Height = _formModel.Height,
                DateOfBirth = _formModel.DateOfBirth.HasValue 
                    ? DateOnly.FromDateTime(_formModel.DateOfBirth.Value) 
                    : null,
                GoalWeight = _formModel.GoalWeight,
                UnitSystem = _formModel.UnitSystem,
                Language = _formModel.Language
            };

            Logger.LogDebug(
                "Profile: Sending update DTO - Name: '{Name}', Height: {Height}cm, GoalWeight: {GoalWeight}kg",
                updateDto.Name, updateDto.Height, updateDto.GoalWeight);

            // 2. Update profile via service
            var updatedUser = await UserService.UpdateProfileAsync(_user.Id, updateDto);

            // 3. Update local state with confirmed values from DB
            _user = updatedUser;
            MapUserDtoToFormModel(updatedUser);

            // 4. Update global Unit System state
            UserStateService.SetCurrentUnitSystem(updatedUser.UnitSystem);

            // 5. Notify other components
            UserStateService.NotifyUserProfileUpdated(updatedUser);

            Logger.LogInformation("Profile: Changes saved successfully - UserId: {UserId}", _user.Id);
            Snackbar.Add(ProfileSavedSuccess, Severity.Success);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error saving changes - UserId: {UserId}", _user.Id);
            Snackbar.Add(ErrorUpdatingProfile, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    /// <summary>
    /// Handles Height field changes with validation (MudTextField workaround).
    /// </summary>
    private Task OnHeightChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _formModel.Height = 170m; // Default
            return Task.CompletedTask;
        }

        if (decimal.TryParse(value, out var height) && height >= 50 && height <= 300)
        {
            _formModel.Height = height;
            Logger.LogDebug("Profile: Height changed to {Height}cm", height);
        }
        else
        {
            Logger.LogWarning("Profile: Invalid height value '{Value}' - must be between 50-300", value);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles GoalWeight field changes with validation (MudTextField workaround).
    /// </summary>
    private Task OnGoalWeightChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _formModel.GoalWeight = null;
            return Task.CompletedTask;
        }

        if (decimal.TryParse(value, out var weight) && weight >= 20 && weight <= 500)
        {
            _formModel.GoalWeight = weight;
            Logger.LogDebug("Profile: GoalWeight changed to {Weight}kg", weight);
        }
        else
        {
            Logger.LogWarning("Profile: Invalid goal weight value '{Value}' - must be between 20-500", value);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles Dark Mode switch toggle.
    /// Applies theme change immediately for better UX.
    /// </summary>
    private async Task OnDarkModeChanged(bool newValue)
    {
        Logger.LogDebug("Profile: Dark mode changed - {Old} → {New}", _formModel.DarkMode, newValue);
        
        _formModel.DarkMode = newValue;

        try
        {
            await ThemeService.SetUserThemePreferenceAsync(newValue);
            UserStateService.NotifyUserThemeUpdated(newValue);

            Logger.LogInformation("Profile: Theme applied immediately - IsDarkMode: {IsDarkMode}", newValue);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error applying theme change");
            Snackbar.Add(ErrorChangingTheme, Severity.Error);
        }

        StateHasChanged();
    }
    
    /// <summary>
    /// Handles Notifications switch toggle.
    /// Persists preference immediately to database.
    /// </summary>
    private async Task OnNotificationsEnabledChanged(bool newValue)
    {
        Logger.LogDebug("Profile: Notifications changed - {Old} → {New}", _formModel.NotificationsEnabled, newValue);
        
        _formModel.NotificationsEnabled = newValue;

        try
        {
            if (_user is not null)
            {
                await UserPreferencesService.UpdateNotificationsEnabledAsync(_user.Id, newValue);
                Logger.LogInformation("Profile: Notifications preference saved - Enabled: {Enabled}", newValue);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error saving notifications preference");
            Snackbar.Add(ErrorChangingNotificationPreference, Severity.Error);
        }

        StateHasChanged();
    }
    
    /// <summary>
    /// Handles external theme updates (e.g., from AppBar button).
    /// </summary>
    private async void OnUserThemeUpdatedExternal(object? sender, bool isDarkMode)
    {
        try
        {
            Logger.LogDebug("Profile: External theme update received - IsDarkMode: {IsDarkMode}", isDarkMode);
            
            _formModel.DarkMode = isDarkMode;
            
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error handling external theme update");
        }
    }

    // ========================================================================
    // EVENT HANDLERS - ACCOUNT ACTIONS
    // ========================================================================
    
    /// <summary>
    /// Signs out the current user.
    /// </summary>
    private async Task SignOut()
    {
        Logger.LogInformation("Profile: User signing out - UserId: {UserId}", _user?.Id);

        try
        {
            NavigationManager.NavigateTo("/logout", forceLoad: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error during sign out");
            Snackbar.Add(ErrorSigningOut, Severity.Error);
        }
    }
    
    /// <summary>
    /// Opens confirmation dialog for account deletion.
    /// </summary>
    private async Task OpenDeleteAccountDialog()
    {
        if (_user is null)
            return;

        Logger.LogInformation("Profile: Opening delete account dialog - UserId: {UserId}", _user.Id);

        var parameters = new DialogParameters
        {
            ["ContentText"] = DeleteAccountConfirmation,
            ["ButtonText"] = DeleteAccountButtonText,
            ["Color"] = Color.Error
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<Components.Shared.ConfirmationDialog>(
            DeleteAccountDialogTitle,
            parameters,
            options);

        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await DeleteUserAccount();
        }
        else
        {
            Logger.LogDebug("Profile: Account deletion cancelled - UserId: {UserId}", _user.Id);
        }
    }
    
    /// <summary>
    /// Deletes user account (not yet implemented).
    /// </summary>
    private async Task DeleteUserAccount()
    {
        if (_user is null)
            return;

        Logger.LogWarning("Profile: Account deletion requested - UserId: {UserId}, Email: {Email}", _user.Id, _user.Email);
        _isSaving = true;

        try
        {
            // TODO: Implement IUserService.DeleteAccountAsync
            Logger.LogError("Profile: Account deletion not implemented - UserId: {UserId}", _user.Id);
            Snackbar.Add(AccountDeletionNotImplemented, Severity.Warning);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error deleting account - UserId: {UserId}", _user.Id);
            Snackbar.Add(ErrorDeletingAccount, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    // ========================================================================
    // EVENT HANDLERS - AVATAR UPLOAD
    // ========================================================================
    
    /// <summary>
    /// Triggers hidden file input for avatar upload.
    /// </summary>
    private async Task TriggerFileInput()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('avatar-file-input').click()");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error triggering file input");
        }
    }
    
    /// <summary>
    /// Handles avatar file selection and opens cropper dialog.
    /// </summary>
    private async Task OnPhotoSelectedAsync(InputFileChangeEventArgs e)
    {
        if (_user is null)
            return;

        Logger.LogInformation("Profile: Avatar file selected - UserId: {UserId}", _user.Id);
        _selectedFile = e.File;

        if (_selectedFile is null)
        {
            Logger.LogWarning("Profile: No file selected");
            return;
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(_selectedFile.ContentType.ToLowerInvariant()))
        {
            Logger.LogWarning("Profile: Invalid file type - ContentType: {ContentType}", _selectedFile.ContentType);
            Snackbar.Add(PhotoInvalidFormat, Severity.Warning);
            _selectedFile = null;
            return;
        }

        // Validate file size (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (_selectedFile.Size > maxFileSize)
        {
            Logger.LogWarning("Profile: File too large - Size: {Size} bytes", _selectedFile.Size);
            Snackbar.Add(PhotoSizeTooLarge, Severity.Warning);
            _selectedFile = null;
            return;
        }

        // Open image cropper dialog
        var parameters = new DialogParameters<Components.Shared.ImageCropperDialog>
        {
            { x => x.SelectedFile, _selectedFile },
            { x => x.MaxFileSize, maxFileSize },
            { x => x.OutputFormat, "image/webp" },
            { x => x.Quality, 0.95 },
            { x => x.OnImageCropped, EventCallback.Factory.Create<string>(this, HandleCroppedImageAsync) }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        Logger.LogDebug("Profile: Opening image cropper dialog - UserId: {UserId}", _user.Id);
        _cropperDialog = await DialogService.ShowAsync<Components.Shared.ImageCropperDialog>(
            CropImageDialogTitle,
            parameters,
            options);

        await _cropperDialog.Result;
    }
    
    /// <summary>
    /// Handles cropped image from dialog and saves to disk + database.
    /// </summary>
    private async Task HandleCroppedImageAsync(string base64Image)
    {
        if (_user is null || string.IsNullOrWhiteSpace(base64Image))
        {
            Logger.LogWarning("Profile: Cannot save cropped image - user null or image empty");
            return;
        }

        Logger.LogInformation("Profile: Saving cropped avatar - UserId: {UserId}", _user.Id);
        _isSaving = true;

        try
        {
            // Extract Base64 data
            var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
            var imageBytes = Convert.FromBase64String(base64Data);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}.webp";
            var uploadsFolder = Path.Combine(WebHostEnvironment.WebRootPath, "uploads", "avatars");
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Ensure directory exists
            Directory.CreateDirectory(uploadsFolder);

            // Save file to disk
            await File.WriteAllBytesAsync(filePath, imageBytes);
            Logger.LogInformation("Profile: Avatar saved to disk - Path: {Path}, Size: {Size} bytes", 
                filePath, imageBytes.Length);

            // Generate relative URL
            var avatarUrl = $"/uploads/avatars/{fileName}";

            // Delete old avatar file if exists
            if (!string.IsNullOrWhiteSpace(_user.AvatarUrl) && _user.AvatarUrl.StartsWith("/uploads/avatars/"))
            {
                var oldFilePath = Path.Combine(WebHostEnvironment.WebRootPath, _user.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    try
                    {
                        File.Delete(oldFilePath);
                        Logger.LogDebug("Profile: Old avatar deleted - Path: {Path}", oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Profile: Could not delete old avatar - Path: {Path}", oldFilePath);
                    }
                }
            }

            // Update user profile with new avatar URL
            var updateDto = new UpdateUserProfileDto
            {
                Name = _formModel.Name.Trim(),
                Height = _formModel.Height,
                DateOfBirth = _formModel.DateOfBirth.HasValue 
                    ? DateOnly.FromDateTime(_formModel.DateOfBirth.Value) 
                    : null,
                GoalWeight = _formModel.GoalWeight,
                UnitSystem = _formModel.UnitSystem,
                Language = _formModel.Language,
                AvatarUrl = avatarUrl
            };

            var updatedUser = await UserService.UpdateProfileAsync(_user.Id, updateDto);
            _user = updatedUser;

            // Update avatar version for cache busting
            _avatarVersion = DateTime.UtcNow.Ticks;

            // Notify other components
            UserStateService.NotifyUserProfileUpdated(_user);

            Logger.LogInformation("Profile: Avatar updated successfully - UserId: {UserId}, URL: {AvatarUrl}", 
                _user.Id, avatarUrl);
            Snackbar.Add(PhotoUploadedSuccess, Severity.Success);

            // Close dialog
            _cropperDialog?.Close(DialogResult.Ok(true));

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error saving avatar - UserId: {UserId}", _user.Id);
            Snackbar.Add(GetErrorUploadingPhoto(ex.Message), Severity.Error);
        }
        finally
        {
            _isSaving = false;
            _selectedFile = null;
            _cropperDialog = null;
        }
    }

    // ========================================================================
    // HELPER METHODS - UI
    // ========================================================================
    
    /// <summary>
    /// Gets avatar URL with cache busting.
    /// Browser will handle 404 if file doesn't exist - no need for File.Exists() check.
    /// </summary>
    private string GetAvatarUrl()
    {
        if (_user is null || string.IsNullOrWhiteSpace(_user.AvatarUrl))
            return string.Empty;

        // Add cache busting to force browser reload
        var separator = _user.AvatarUrl.Contains('?') ? '&' : '?';
        var avatarUrlWithCache = $"{_user.AvatarUrl}{separator}v={_avatarVersion}";

        Logger.LogDebug("Profile: GetAvatarUrl returning - URL: {Url}, AvatarVersion: {Version}", 
            avatarUrlWithCache, _avatarVersion);

        return avatarUrlWithCache;
    }
    
    /// <summary>
    /// Gets role badge color.
    /// </summary>
    private Color GetRoleColor() => _user?.Role switch
    {
        UserRole.Administrator => Color.Error,
        UserRole.User => Color.Primary,
        _ => Color.Default
    };
}
