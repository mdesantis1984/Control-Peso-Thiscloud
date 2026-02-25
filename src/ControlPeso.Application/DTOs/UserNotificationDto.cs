using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO for user notification (historical record)
/// </summary>
public sealed class UserNotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationSeverity Type { get; set; }
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// DTO for creating a new user notification
/// </summary>
public sealed class CreateUserNotificationDto
{
    public Guid UserId { get; set; }
    public NotificationSeverity Type { get; set; }
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
}
