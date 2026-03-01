using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Services;

/// <summary>
/// Service for managing user notification history with persistent storage
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    private readonly IDbContextFactory<ControlPesoDbContext> _contextFactory;
    private readonly ILogger<UserNotificationService> _logger;

    public UserNotificationService(
        IDbContextFactory<ControlPesoDbContext> contextFactory,
        ILogger<UserNotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<UserNotificationDto>> GetUnreadAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting unread notifications for user {UserId}", userId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var notifications = await context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to most recent 50 unread
                .ToListAsync(ct);

            var result = notifications.Select(MapToDto).ToList();

            _logger.LogInformation(
                "Retrieved {Count} unread notifications for user {UserId}",
                result.Count, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PagedResult<UserNotificationDto>> GetAllAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting all notifications for user {UserId} - Page: {Page}, PageSize: {PageSize}",
            userId, page, pageSize);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            var totalCount = await query.CountAsync(ct);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = notifications.Select(MapToDto).ToList();

            var result = new PagedResult<UserNotificationDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation(
                "Retrieved {Count} of {Total} notifications for user {UserId}",
                items.Count, totalCount, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting notifications for user {UserId} - Page: {Page}",
                userId, page);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting unread count for user {UserId}", userId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var count = await context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync(ct);

            _logger.LogDebug("User {UserId} has {Count} unread notifications", userId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserNotificationDto> CreateAsync(
        CreateUserNotificationDto dto,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        _logger.LogInformation(
            "Creating notification for user {UserId} - Type: {Type}",
            dto.UserId, dto.Type);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = new UserNotifications
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Type = dto.Type.ToString(),
                Title = dto.Title ?? string.Empty,
                Message = dto.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserNotifications.Add(entity);
            await context.SaveChangesAsync(ct);

            var result = MapToDto(entity);

            _logger.LogInformation(
                "Notification created - Id: {NotificationId}, UserId: {UserId}",
                result.Id, dto.UserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating notification for user {UserId}",
                dto.UserId);
            throw;
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var notification = await context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return;
            }

            if (notification.IsRead)
            {
                _logger.LogDebug("Notification {NotificationId} already marked as read", notificationId);
                return;
            }

            notification.IsRead = true;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var unreadNotifications = await context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(ct);

            if (unreadNotifications.Count == 0)
            {
                _logger.LogDebug("No unread notifications to mark for user {UserId}", userId);
                return;
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Marked {Count} notifications as read for user {UserId}",
                unreadNotifications.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting notification {NotificationId}", notificationId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var notification = await context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return;
            }

            context.UserNotifications.Remove(notification);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Notification {NotificationId} deleted", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task DeleteAllAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting all notifications for user {UserId}", userId);

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var notifications = await context.UserNotifications
                .Where(n => n.UserId == userId)
                .ToListAsync(ct);

            if (notifications.Count == 0)
            {
                _logger.LogDebug("No notifications to delete for user {UserId}", userId);
                return;
            }

            context.UserNotifications.RemoveRange(notifications);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Deleted {Count} notifications for user {UserId}",
                notifications.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Map scaffolded entity to DTO with type conversions
    /// </summary>
    private static UserNotificationDto MapToDto(UserNotifications entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Parse Type string to NotificationSeverity enum
        if (!Enum.TryParse<NotificationSeverity>(entity.Type, out var severity))
        {
            severity = NotificationSeverity.Info; // Default fallback
        }

        return new UserNotificationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Type = severity,
            Title = entity.Title,
            Message = entity.Message,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt,
            ReadAt = null // ReadAt property no longer exists in entity
        };
    }
}
