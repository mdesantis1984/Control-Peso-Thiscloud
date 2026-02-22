using System.Security.Claims;
using ControlPeso.Application.DTOs;
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
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<Profile> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isSavingPreferences;

    private UserDto? _user;
    private IBrowserFile? _selectedFile;
    private IDialogReference? _cropperDialog;
    
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
            
            Logger.LogInformation("Profile updated successfully - UserId: {UserId}", _user.Id);
            Snackbar.Add("Perfil actualizado correctamente", Severity.Success);
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

    private async Task SavePreferences()
    {
        if (_user is null) return;

        Logger.LogInformation("Saving preferences for user {UserId}", _user.Id);
        _isSavingPreferences = true;

        try
        {
            // TODO: Implementar UserPreferencesService cuando esté disponible
            // Por ahora solo simulamos el guardado
            await Task.Delay(500);

            Logger.LogInformation("Preferences saved successfully - UserId: {UserId}, DarkMode: {DarkMode}, Notifications: {Notifications}",
                _user.Id, _darkMode, _notificationsEnabled);
            
            Snackbar.Add("Preferencias guardadas correctamente", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving preferences - UserId: {UserId}", _user.Id);
            Snackbar.Add("Error al guardar las preferencias", Severity.Error);
        }
        finally
        {
            _isSavingPreferences = false;
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

            Logger.LogInformation("Avatar updated successfully - UserId: {UserId}, AvatarUrl: {AvatarUrl}", _user.Id, avatarUrl);
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
}
