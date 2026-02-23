using System.Security.Claims;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Pages;

public partial class Profile : IDisposable
{
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

    private bool _isLoading = true;
    private bool _isSaving;

    private UserDto? _user;
    private IBrowserFile? _selectedFile;
    private IDialogReference? _cropperDialog;
    private WeightStatsDto? _stats;
    private long _avatarVersion = DateTime.UtcNow.Ticks; // Cache busting for avatar image

    // Form fields
    private string _name = string.Empty;
    private decimal _height = 170m;
    private DateTime? _dateOfBirth;
    private decimal? _goalWeight;
    private UnitSystem _unitSystem = UnitSystem.Metric;
    private string _language = "es";

    // Preferences (propiedades públicas para @bind-Checked)
    private bool _darkModeBackingField = true;
    public bool DarkMode 
    { 
        get => _darkModeBackingField;
        set 
        {
            Logger.LogWarning("🔥🔥🔥 DarkMode SETTER CALLED - Old: {Old}, New: {New}", _darkModeBackingField, value);
            _darkModeBackingField = value;
            StateHasChanged(); // Force UI update
        }
    }

    private bool _notificationsEnabledBackingField = true;
    public bool NotificationsEnabled 
    { 
        get => _notificationsEnabledBackingField;
        set 
        {
            Logger.LogWarning("🔥🔥🔥 NotificationsEnabled SETTER CALLED - Old: {Old}, New: {New}", _notificationsEnabledBackingField, value);
            _notificationsEnabledBackingField = value;
            StateHasChanged(); // Force UI update
        }
    }

    // ========================================================================
    // LOCALIZED STRINGS
    // ========================================================================

    // Page & Meta
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];

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

    // Event handlers for MudSwitch ValueChanged (bypasses broken @bind-Checked)
    private async void OnDarkModeChanged(bool newValue)
    {
        Logger.LogWarning("🔥🔥🔥 OnDarkModeChanged CALLED - Old: {Old}, New: {New}", _darkModeBackingField, newValue);

        // Update backing field
        _darkModeBackingField = newValue;

        // Apply theme IMMEDIATELY (user experience improvement)
        try
        {
            await ThemeService.SetUserThemePreferenceAsync(newValue);
            UserStateService.NotifyUserThemeUpdated(newValue);

            Logger.LogInformation("Profile: Theme changed immediately - IsDarkMode: {IsDarkMode}", newValue);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error applying theme immediately");
            Snackbar.Add(ErrorChangingTheme, Severity.Error);
        }

        StateHasChanged();
    }

    private async void OnNotificationsEnabledChanged(bool newValue)
    {
        Logger.LogWarning("🔥🔥🔥 OnNotificationsEnabledChanged CALLED - Old: {Old}, New: {New}", _notificationsEnabledBackingField, newValue);

        // Update backing field
        _notificationsEnabledBackingField = newValue;

        // Apply notification preference IMMEDIATELY (user experience improvement)
        // Note: Full notification provider architecture (SMTP, Telegram, Push, Alert, Snackbar) 
        // is future work. For now, persist the preference to database.
        try
        {
            if (_user != null)
            {
                await UserPreferencesService.UpdateNotificationsEnabledAsync(_user.Id, newValue);
                Logger.LogInformation("Profile: Notifications preference changed immediately - Enabled: {Enabled}", newValue);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error applying notifications preference immediately");
            Snackbar.Add(ErrorChangingNotificationPreference, Severity.Error);
        }

        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading user profile");

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Logger.LogWarning("User ID claim not found or invalid");
                Snackbar.Add(ErrorUserNotIdentified, Severity.Error);
                return;
            }

            Logger.LogDebug("Loading profile for user {UserId}", userId);
            _user = await UserService.GetByIdAsync(userId);

            if (_user is null)
            {
                Logger.LogWarning("User {UserId} not found in database", userId);
                Snackbar.Add(ErrorUserNotFound, Severity.Error);
                return;
            }

            // Populate form fields
            _name = _user.Name;
            _height = _user.Height;
            _dateOfBirth = _user.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
            _goalWeight = _user.GoalWeight;
            _unitSystem = _user.UnitSystem;
            _language = _user.Language;

            // Load user preferences (Dark Mode, Notifications)
            try
            {
                Logger.LogInformation("🔍 BEFORE LOADING PREFERENCES - UserId: {UserId}", userId);

                _darkModeBackingField = await UserPreferencesService.GetDarkModePreferenceAsync(userId);
                _notificationsEnabledBackingField = await UserPreferencesService.GetNotificationsEnabledAsync(userId);

                Logger.LogInformation("✅ PREFERENCES LOADED - UserId: {UserId}, DarkMode: {DarkMode}, Notifications: {Notifications}",
                    userId, _darkModeBackingField, _notificationsEnabledBackingField);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ ERROR LOADING PREFERENCES - UserId: {UserId}", userId);
                // Continue with defaults
                _darkModeBackingField = true;
                _notificationsEnabledBackingField = true;
            }

            // DEBUG: Log avatar URL
            Logger.LogInformation("Profile loaded - AvatarUrl from DB: '{AvatarUrl}' (IsNullOrWhiteSpace: {IsEmpty})", 
                _user.AvatarUrl ?? "(null)", 
                string.IsNullOrWhiteSpace(_user.AvatarUrl));

            // DEBUG: Check if avatar file exists on disk
            if (!string.IsNullOrWhiteSpace(_user.AvatarUrl) && _user.AvatarUrl.StartsWith("/uploads/avatars/"))
            {
                var filePath = Path.Combine("wwwroot", _user.AvatarUrl.TrimStart('/'));
                var fileExists = File.Exists(filePath);
                Logger.LogInformation("Avatar file check - Path: '{Path}', Exists: {Exists}", filePath, fileExists);
            }

            // Load weight statistics (all time)
            try
            {
                _stats = await WeightLogService.GetStatsAsync(
                    userId,
                    new Application.Filters.DateRange
                    {
                        StartDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10)),
                        EndDate = DateOnly.FromDateTime(DateTime.Today)
                    });

                Logger.LogDebug("Weight stats loaded - Current: {Current}, Starting: {Starting}, Change: {Change}",
                    _stats?.CurrentWeight, _stats?.StartingWeight, _stats?.TotalChange);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not load weight statistics for user {UserId}", userId);
                // Continue without stats - not critical
            }

            Logger.LogInformation("User profile loaded successfully - UserId: {UserId}, Name: {Name}", userId, _user.Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading user profile");
            Snackbar.Add(ErrorLoadingProfileGeneral, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveChanges()
    {
        if (_user is null) return;

        Logger.LogInformation("Saving profile changes for user {UserId}", _user.Id);
        _isSaving = true;

        try
        {
            // 1. Update user profile (name, height, goals, etc.)
            var dto = new UpdateUserProfileDto
            {
                Name = _name.Trim(),
                Height = _height,
                DateOfBirth = _dateOfBirth.HasValue ? DateOnly.FromDateTime(_dateOfBirth.Value) : null,
                GoalWeight = _goalWeight,
                UnitSystem = _unitSystem,
                Language = _language
            };

            var updatedUser = await UserService.UpdateProfileAsync(_user.Id, dto);
            _user = updatedUser;

            // Notify other components that profile changed
            UserStateService.NotifyUserProfileUpdated(_user);

            // NOTE: Dark Mode and Notifications preferences are already saved immediately
            // by OnDarkModeChanged and OnNotificationsEnabledChanged when switches are toggled.
            // No need to save them again here (avoids duplicate DB writes).

            Logger.LogInformation("Profile updated successfully - UserId: {UserId}", _user.Id);
            Snackbar.Add(ProfileSavedSuccess, Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user profile - UserId: {UserId}", _user.Id);
            Snackbar.Add(ErrorUpdatingProfile, Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task SignOut()
    {
        Logger.LogInformation("User {UserId} signing out", _user?.Id);

        try
        {
            // Navigate to logout page (Google OAuth logout)
            NavigationManager.NavigateTo("/logout", forceLoad: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during sign out");
            Snackbar.Add(ErrorSigningOut, Severity.Error);
        }
    }

    private async Task OpenDeleteAccountDialog()
    {
        if (_user is null) return;

        Logger.LogInformation("Opening delete account confirmation for user {UserId}", _user.Id);

        // Create custom confirmation dialog
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

        if (!result.Canceled)
        {
            await DeleteUserAccount();
        }
        else
        {
            Logger.LogInformation("User {UserId} cancelled account deletion", _user.Id);
        }
    }

    private async Task DeleteUserAccount()
    {
        if (_user is null) return;

        Logger.LogWarning("User {UserId} ({Email}) is deleting their account", _user.Id, _user.Email);
        _isSaving = true;

        try
        {
            // TODO: Implement IUserService.DeleteAccountAsync method
            // This should:
            // 1. Delete all WeightLogs for this user
            // 2. Delete UserPreferences for this user
            // 3. Delete AuditLogs for this user (optional - keep for auditing)
            // 4. Delete avatar file if exists
            // 5. Delete User record
            // 6. Sign out user
            // 7. Redirect to home page

            // For now, show a message that deletion is not implemented
            Logger.LogError("Account deletion not implemented yet - UserId: {UserId}", _user.Id);
            Snackbar.Add(AccountDeletionNotImplemented, Severity.Warning);

            // When implemented:
            // await UserService.DeleteAccountAsync(_user.Id);
            // Logger.LogWarning("Account deleted successfully - UserId: {UserId}, Email: {Email}", _user.Id, _user.Email);
            // Snackbar.Add("Tu cuenta ha sido eliminada", Severity.Info);
            // await Task.Delay(2000);
            // NavigationManager.NavigateTo("/logout", forceLoad: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting user account - UserId: {UserId}", _user.Id);
            Snackbar.Add("Error al eliminar la cuenta", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private Color GetRoleColor() => _user?.Role switch
    {
        UserRole.Administrator => Color.Error,
        UserRole.User => Color.Primary,
        _ => Color.Default
    };

    private async Task TriggerFileInput()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('avatar-file-input').click()");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error triggering file input");
        }
    }

    private async Task OnPhotoSelectedAsync(InputFileChangeEventArgs e)
    {
        if (_user is null) return;

        Logger.LogInformation("Photo selected for user {UserId}", _user.Id);
        _selectedFile = e.File;

        if (_selectedFile is null)
        {
            Logger.LogWarning("No file selected");
            return;
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(_selectedFile.ContentType.ToLowerInvariant()))
        {
            Logger.LogWarning("Invalid file type: {ContentType}", _selectedFile.ContentType);
            Snackbar.Add(PhotoInvalidFormat, Severity.Warning);
            _selectedFile = null;
            return;
        }

        // Validate file size (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (_selectedFile.Size > maxFileSize)
        {
            Logger.LogWarning("File too large: {Size} bytes", _selectedFile.Size);
            Snackbar.Add(PhotoSizeTooLarge, Severity.Warning);
            _selectedFile = null;
            return;
        }

        // Open ImageCropperDialog
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

        Logger.LogDebug("Opening ImageCropperDialog for user {UserId}", _user.Id);
        _cropperDialog = await DialogService.ShowAsync<Components.Shared.ImageCropperDialog>(
            CropImageDialogTitle,
            parameters,
            options);

        await _cropperDialog.Result;
    }

    private async Task HandleCroppedImageAsync(string base64Image)
    {
        if (_user is null || string.IsNullOrWhiteSpace(base64Image))
        {
            Logger.LogWarning("Cannot save cropped image: user is null or base64Image is empty");
            return;
        }

        Logger.LogInformation("Saving cropped image for user {UserId}", _user.Id);
        _isSaving = true;

        try
        {
            // Extract Base64 data (remove data:image/...;base64, prefix)
            var base64Data = base64Image.Contains(",") 
                ? base64Image.Split(',')[1] 
                : base64Image;

            // Decode Base64 to bytes
            var imageBytes = Convert.FromBase64String(base64Data);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}.webp";
            var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
            var filePath = Path.Combine(uploadsFolder, fileName);

            Logger.LogInformation("=== SAVING AVATAR FILE ===");
            Logger.LogInformation("Filename: {FileName}", fileName);
            Logger.LogInformation("Uploads folder: {UploadsFolder}", uploadsFolder);
            Logger.LogInformation("Full file path: {FilePath}", filePath);

            // Ensure directory exists
            Directory.CreateDirectory(uploadsFolder);
            Logger.LogInformation("Directory created/verified: {UploadsFolder}", uploadsFolder);

            // Save file to disk
            await File.WriteAllBytesAsync(filePath, imageBytes);
            Logger.LogInformation("✅ File written to disk: {FilePath} ({Size} bytes)", filePath, imageBytes.Length);

            // Verify file was saved
            var fileExists = File.Exists(filePath);
            Logger.LogInformation("File exists after save: {Exists}", fileExists);

            // Generate relative URL for database
            var avatarUrl = $"/uploads/avatars/{fileName}";

            Logger.LogInformation("Generated avatar URL for DB: '{AvatarUrl}'", avatarUrl);

            // Update user profile with file URL (not Base64)
            var dto = new UpdateUserProfileDto
            {
                Name = _name.Trim(),
                Height = _height,
                DateOfBirth = _dateOfBirth.HasValue ? DateOnly.FromDateTime(_dateOfBirth.Value) : null,
                GoalWeight = _goalWeight,
                UnitSystem = _unitSystem,
                Language = _language,
                AvatarUrl = avatarUrl // Save URL, not Base64
            };

            // Delete old avatar file if exists and is not a Google avatar
            if (!string.IsNullOrWhiteSpace(_user.AvatarUrl) && 
                _user.AvatarUrl.StartsWith("/uploads/avatars/"))
            {
                var oldFilePath = Path.Combine("wwwroot", _user.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    try
                    {
                        File.Delete(oldFilePath);
                        Logger.LogInformation("Deleted old avatar file: {OldFilePath}", oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Could not delete old avatar file: {OldFilePath}", oldFilePath);
                    }
                }
            }

            var updatedUser = await UserService.UpdateProfileAsync(_user.Id, dto);
            _user = updatedUser;

            // Update avatar version to force browser reload (cache busting)
            _avatarVersion = DateTime.UtcNow.Ticks;

            // Notify other components (e.g., MainLayout) that avatar changed
            UserStateService.NotifyUserProfileUpdated(_user);

            Logger.LogInformation("Avatar updated successfully - UserId: {UserId}, AvatarUrl: {AvatarUrl}, Version: {Version}", 
                _user.Id, avatarUrl, _avatarVersion);
            Snackbar.Add(PhotoUploadedSuccess, Severity.Success);

            // Close the cropper dialog after successful save
            if (_cropperDialog is not null)
            {
                _cropperDialog.Close(DialogResult.Ok(true));
                Logger.LogDebug("Cropper dialog closed successfully");
            }

            // Force UI refresh
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating avatar - UserId: {UserId}", _user.Id);
            Snackbar.Add(GetErrorUploadingPhoto(ex.Message), Severity.Error);
        }
        finally
        {
            _isSaving = false;
            _selectedFile = null;
            _cropperDialog = null; // Clear dialog reference
        }
    }

    /// <summary>
    /// Gets avatar URL with cache busting query parameter to force browser reload after update.
    /// Returns empty string if file doesn't exist on disk.
    /// </summary>
    private string GetAvatarUrl()
    {
        Logger.LogInformation("=== GetAvatarUrl CALLED ===");
        Logger.LogInformation("_user is null: {IsNull}", _user is null);

        if (_user is null || string.IsNullOrWhiteSpace(_user.AvatarUrl))
        {
            Logger.LogWarning("GetAvatarUrl: Returning empty - User: {UserNull}, AvatarUrl: '{AvatarUrl}'", 
                _user is null ? "NULL" : "not null", 
                _user?.AvatarUrl ?? "(null)");
            return string.Empty;
        }

        Logger.LogInformation("GetAvatarUrl: User has AvatarUrl: '{AvatarUrl}'", _user.AvatarUrl);

        // If it's a local avatar (not Google URL), verify file exists
        if (_user.AvatarUrl.StartsWith("/uploads/avatars/"))
        {
            var filePath = Path.Combine("wwwroot", _user.AvatarUrl.TrimStart('/'));
            var fileExists = File.Exists(filePath);

            Logger.LogInformation("GetAvatarUrl: Checking local file - Path: '{Path}', Exists: {Exists}", filePath, fileExists);

            if (!fileExists)
            {
                Logger.LogError("GetAvatarUrl: ❌ LOCAL AVATAR FILE DOES NOT EXIST - Path: '{Path}', Returning empty to show initials", filePath);
                return string.Empty;
            }

            Logger.LogInformation("GetAvatarUrl: ✅ File exists, continuing...");
        }
        else
        {
            Logger.LogInformation("GetAvatarUrl: External URL (Google/etc), not checking file existence");
        }

        // Add version query string to prevent browser caching
        var separator = _user.AvatarUrl.Contains('?') ? '&' : '?';
        var url = $"{_user.AvatarUrl}{separator}v={_avatarVersion}";

        Logger.LogInformation("GetAvatarUrl: ✅ Returning URL: '{Url}' (Base: '{Base}', Version: {Version})", 
            url, _user.AvatarUrl, _avatarVersion);
        return url;
    }

    /// <summary>
    /// Handler para cambios de tema desde otros componentes (MainLayout AppBar button).
    /// Mantiene sincronizados los switches de Profile con el botón de AppBar.
    /// </summary>
    private async void OnUserThemeUpdated(object? sender, bool isDarkMode)
    {
        try
        {
            Logger.LogInformation("Profile: User theme updated externally - IsDarkMode: {IsDarkMode}", isDarkMode);

            DarkMode = isDarkMode;

            await InvokeAsync(StateHasChanged); // Force re-render to update switches
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Profile: Error handling user theme update");
        }
    }

    public void Dispose()
    {
        UserStateService.UserThemeUpdated -= OnUserThemeUpdated;
    }
}
