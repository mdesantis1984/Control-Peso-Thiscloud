using ControlPeso.Application.DTOs;

namespace ControlPeso.Web.Services;

/// <summary>
/// Service to manage shared user state across components.
/// Notifies subscribers when user profile changes (e.g., avatar update).
/// </summary>
public sealed class UserStateService
{
    private readonly ILogger<UserStateService> _logger;

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
    /// Notify all subscribers that user profile has been updated.
    /// </summary>
    public void NotifyUserProfileUpdated(UserDto updatedUser)
    {
        ArgumentNullException.ThrowIfNull(updatedUser);

        _logger.LogInformation(
            "UserStateService: Notifying profile update - UserId: {UserId}, AvatarUrl: {AvatarUrl}",
            updatedUser.Id,
            updatedUser.AvatarUrl ?? "(null)");

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
}
