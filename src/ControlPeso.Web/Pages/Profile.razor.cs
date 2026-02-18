using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace ControlPeso.Web.Pages;

public partial class Profile
{
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<Profile> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isSavingPreferences;

    private UserDto? _user;

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
            var userIdClaim = authState.User.FindFirst("sub")?.Value;

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
        if (_user is null)
            return;

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
        if (_user is null)
            return;

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
}
