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
}
