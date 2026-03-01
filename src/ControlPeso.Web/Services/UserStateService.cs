using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Enums;

namespace ControlPeso.Web.Services;

/// <summary>
/// Service to manage shared user state across components.
/// Notifies subscribers when user profile changes (e.g., avatar update, unit system).
/// Provides global access to user preferences like Unit System (Metric/Imperial).
/// </summary>
public sealed class UserStateService
{
    private readonly ILogger<UserStateService> _logger;

    // Global state: Unit System (accessible by all components)
    private UnitSystem _currentUnitSystem = UnitSystem.Metric;

    public UserStateService(ILogger<UserStateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Event raised when user profile is updated.
    /// Subscribers (e.g., MainLayout) can refresh their local user data.
    /// </summary>
    public event EventHandler<UserDto>? UserProfileUpdated;

    /// <summary>
    /// Event raised when user theme preference (Dark Mode) is updated.
    /// Subscribers (e.g., MainLayout, Profile page) can refresh their UI.
    /// </summary>
    public event EventHandler<bool>? UserThemeUpdated;

    /// <summary>
    /// Event raised when user unit system (Metric/Imperial) is updated.
    /// Subscribers (e.g., Dashboard, History, Charts) can refresh displayed units.
    /// </summary>
    public event EventHandler<UnitSystem>? UserUnitSystemUpdated;

    /// <summary>
    /// Gets the current user's unit system preference (Metric or Imperial).
    /// This is globally accessible by all components for weight/height conversions.
    /// </summary>
    public UnitSystem CurrentUnitSystem => _currentUnitSystem;

    /// <summary>
    /// Sets the current user's unit system preference.
    /// Call this when user profile loads or changes in Profile page.
    /// </summary>
    public void SetCurrentUnitSystem(UnitSystem unitSystem)
    {
        if (_currentUnitSystem != unitSystem)
        {
            _logger.LogInformation(
                "UserStateService: Unit system changed - Old: {Old}, New: {New}",
                _currentUnitSystem, unitSystem);

            _currentUnitSystem = unitSystem;
            UserUnitSystemUpdated?.Invoke(this, unitSystem);
        }
    }

    /// <summary>
    /// Notify all subscribers that user profile has been updated.
    /// </summary>
    public void NotifyUserProfileUpdated(UserDto updatedUser)
    {
        ArgumentNullException.ThrowIfNull(updatedUser);

        _logger.LogInformation(
            "UserStateService: Notifying profile update - UserId: {UserId}, AvatarUrl: {AvatarUrl}, UnitSystem: {UnitSystem}",
            updatedUser.Id,
            updatedUser.AvatarUrl ?? "(null)",
            updatedUser.UnitSystem);

        // Update global unit system state
        SetCurrentUnitSystem(updatedUser.UnitSystem);

        UserProfileUpdated?.Invoke(this, updatedUser);
    }

    /// <summary>
    /// Notify all subscribers that user theme preference has been updated.
    /// </summary>
    /// <param name="isDarkMode">True for dark mode, false for light mode</param>
    public void NotifyUserThemeUpdated(bool isDarkMode)
    {
        _logger.LogInformation(
            "UserStateService: Notifying theme update - IsDarkMode: {IsDarkMode}",
            isDarkMode);

        UserThemeUpdated?.Invoke(this, isDarkMode);
    }

    /// <summary>
    /// Converts weight from kg to the user's preferred unit (kg or lb).
    /// </summary>
    public decimal ConvertWeight(decimal weightInKg)
    {
        return _currentUnitSystem == UnitSystem.Imperial
            ? weightInKg * 2.20462m  // kg → lb
            : weightInKg;             // kg → kg (no conversion)
    }

    /// <summary>
    /// Converts height from cm to the user's preferred unit (cm or in).
    /// </summary>
    public decimal ConvertHeight(decimal heightInCm)
    {
        return _currentUnitSystem == UnitSystem.Imperial
            ? heightInCm / 2.54m      // cm → in
            : heightInCm;             // cm → cm (no conversion)
    }

    /// <summary>
    /// Gets the weight unit label (kg or lb) based on user's preference.
    /// </summary>
    public string GetWeightUnitLabel()
    {
        return _currentUnitSystem == UnitSystem.Imperial ? "lb" : "kg";
    }

    /// <summary>
    /// Gets the height unit label (cm or in) based on user's preference.
    /// </summary>
    public string GetHeightUnitLabel()
    {
        return _currentUnitSystem == UnitSystem.Imperial ? "in" : "cm";
    }
}
