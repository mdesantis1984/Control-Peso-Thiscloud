using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;

namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service for managing user notification history
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Get unread notifications for a user
    /// </summary>
    Task<List<UserNotificationDto>> GetUnreadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Get all notifications for a user (paginated)
    /// </summary>
    Task<PagedResult<UserNotificationDto>> GetAllAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Get count of unread notifications for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Create a new notification
    /// </summary>
    Task<UserNotificationDto> CreateAsync(CreateUserNotificationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Mark all notifications for a user as read
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a specific notification
    /// </summary>
    Task DeleteAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Delete all notifications for a user
    /// </summary>
    Task DeleteAllAsync(Guid userId, CancellationToken ct = default);
}
