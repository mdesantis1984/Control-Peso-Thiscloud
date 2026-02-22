using System.Security.Claims;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Pages;

public partial class Profile
{
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ILogger<Profile> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private Services.UserStateService UserStateService { get; set; } = null!;

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

    // Preferences
    private bool _darkMode = true;
    private bool _notificationsEnabled = true;

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
                Snackbar.Add("No se pudo identificar al usuario", Severity.Error);
                return;
            }

            Logger.LogDebug("Loading profile for user {UserId}", userId);
            _user = await UserService.GetByIdAsync(userId);

            if (_user is null)
            {
                Logger.LogWarning("User {UserId} not found in database", userId);
                Snackbar.Add("Usuario no encontrado", Severity.Error);
                return;
            }

            // Populate form fields
            _name = _user.Name;
            _height = _user.Height;
            _dateOfBirth = _user.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
            _goalWeight = _user.GoalWeight;
            _unitSystem = _user.UnitSystem;
            _language = _user.Language;

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
            Snackbar.Add("Error al cargar el perfil", Severity.Error);
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

            // TODO: Save preferences (dark mode, notifications) when UserPreferencesService is available

            Logger.LogInformation("Profile updated successfully - UserId: {UserId}", _user.Id);
            Snackbar.Add("Profile updated successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user profile - UserId: {UserId}", _user.Id);
            Snackbar.Add("Error al actualizar el perfil", Severity.Error);
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
            Snackbar.Add("Error al cerrar sesión", Severity.Error);
        }
    }

    private async Task OpenChangePasswordDialog()
    {
        Logger.LogInformation("Opening change password dialog for user {UserId}", _user?.Id);

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small
        };

        var parameters = new DialogParameters
        {
            ["ContentText"] = "This feature is not available for Google OAuth users. Please manage your password through your Google account settings.",
            ["CloseText"] = "OK"
        };

        await DialogService.ShowAsync<MudMessageBox>("Change Password", parameters, options);
    }

    private async Task OpenDeleteAccountDialog()
    {
        Logger.LogInformation("Opening delete account confirmation for user {UserId}", _user?.Id);

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small
        };

        var parameters = new DialogParameters
        {
            ["ContentText"] = "Are you sure you want to permanently delete your account? This action cannot be undone and all your data will be lost.",
            ["CancelText"] = "Cancel",
            ["YesText"] = "Delete"
        };

        var dialog = await DialogService.ShowAsync<MudMessageBox>("Delete Account", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            Logger.LogWarning("User {UserId} confirmed account deletion", _user?.Id);
            Snackbar.Add("Account deletion is not yet implemented", Severity.Warning);
            // TODO: Implement account deletion when service is available
        }
        else
        {
            Logger.LogInformation("User {UserId} cancelled account deletion", _user?.Id);
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
            Snackbar.Add("Solo se permiten imágenes (JPG, PNG, WebP)", Severity.Warning);
            _selectedFile = null;
            return;
        }

        // Validate file size (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (_selectedFile.Size > maxFileSize)
        {
            Logger.LogWarning("File too large: {Size} bytes", _selectedFile.Size);
            Snackbar.Add("La imagen no debe superar 5MB", Severity.Warning);
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
            "Recortar Imagen",
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

            // Ensure directory exists
            Directory.CreateDirectory(uploadsFolder);

            // Save file to disk
            await File.WriteAllBytesAsync(filePath, imageBytes);

            // Generate relative URL for database
            var avatarUrl = $"/uploads/avatars/{fileName}";

            Logger.LogInformation("Image saved to disk: {FilePath}, URL: {AvatarUrl}", filePath, avatarUrl);

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
            Snackbar.Add("Foto de perfil actualizada correctamente", Severity.Success);

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
            Snackbar.Add("Error al actualizar la foto de perfil", Severity.Error);
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
        if (_user is null || string.IsNullOrWhiteSpace(_user.AvatarUrl))
        {
            Logger.LogDebug("GetAvatarUrl: Returning empty - User: {UserNull}, AvatarUrl: '{AvatarUrl}'", 
                _user is null ? "null" : "not null", 
                _user?.AvatarUrl ?? "(null)");
            return string.Empty;
        }

        // If it's a local avatar (not Google URL), verify file exists
        if (_user.AvatarUrl.StartsWith("/uploads/avatars/"))
        {
            var filePath = Path.Combine("wwwroot", _user.AvatarUrl.TrimStart('/'));
            if (!File.Exists(filePath))
            {
                Logger.LogWarning("GetAvatarUrl: Avatar file does not exist - Path: '{Path}', Returning empty to show initials", filePath);
                return string.Empty;
            }
        }

        // Add version query string to prevent browser caching
        var separator = _user.AvatarUrl.Contains('?') ? '&' : '?';
        var url = $"{_user.AvatarUrl}{separator}v={_avatarVersion}";

        Logger.LogDebug("GetAvatarUrl: Returning '{Url}' (Base: '{Base}', Version: {Version})", 
            url, _user.AvatarUrl, _avatarVersion);
        return url;
    }
}
