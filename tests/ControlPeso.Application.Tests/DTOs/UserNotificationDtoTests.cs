using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Application.Tests.DTOs;

/// <summary>
/// Tests for UserNotificationDto and CreateUserNotificationDto.
/// Simple property validation to ensure DTOs work correctly.
/// </summary>
public sealed class UserNotificationDtoTests
{
    [Fact]
    public void UserNotificationDto_CanBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var readAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var dto = new UserNotificationDto
        {
            Id = id,
            UserId = userId,
            Type = NotificationSeverity.Info,
            Title = "Test Title",
            Message = "Test Message",
            IsRead = true,
            CreatedAt = createdAt,
            ReadAt = readAt
        };

        // Assert
        dto.Id.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.Type.Should().Be(NotificationSeverity.Info);
        dto.Title.Should().Be("Test Title");
        dto.Message.Should().Be("Test Message");
        dto.IsRead.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
        dto.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public void UserNotificationDto_CanBeCreatedWithNullableProperties()
    {
        // Arrange & Act
        var dto = new UserNotificationDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = NotificationSeverity.Warning,
            Title = null, // Nullable
            Message = "Warning message",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ReadAt = null // Nullable
        };

        // Assert
        dto.Title.Should().BeNull();
        dto.ReadAt.Should().BeNull();
        dto.IsRead.Should().BeFalse();
    }

    [Fact]
    public void UserNotificationDto_MessageHasDefaultValue()
    {
        // Arrange & Act
        var dto = new UserNotificationDto();

        // Assert
        dto.Message.Should().Be(string.Empty);
        dto.IsRead.Should().BeFalse(); // Default for bool
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public void UserNotificationDto_SupportsAllSeverityTypes(NotificationSeverity severity)
    {
        // Arrange & Act
        var dto = new UserNotificationDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = severity,
            Message = "Test",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Type.Should().Be(severity);
    }

    [Fact]
    public void CreateUserNotificationDto_CanBeCreatedWithAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var dto = new CreateUserNotificationDto
        {
            UserId = userId,
            Type = NotificationSeverity.Success,
            Title = "Success Title",
            Message = "Success Message"
        };

        // Assert
        dto.UserId.Should().Be(userId);
        dto.Type.Should().Be(NotificationSeverity.Success);
        dto.Title.Should().Be("Success Title");
        dto.Message.Should().Be("Success Message");
    }

    [Fact]
    public void CreateUserNotificationDto_CanBeCreatedWithNullTitle()
    {
        // Arrange & Act
        var dto = new CreateUserNotificationDto
        {
            UserId = Guid.NewGuid(),
            Type = NotificationSeverity.Error,
            Title = null, // Nullable
            Message = "Error occurred"
        };

        // Assert
        dto.Title.Should().BeNull();
        dto.Message.Should().Be("Error occurred");
    }

    [Fact]
    public void CreateUserNotificationDto_MessageHasDefaultValue()
    {
        // Arrange & Act
        var dto = new CreateUserNotificationDto();

        // Assert
        dto.Message.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public void CreateUserNotificationDto_SupportsAllSeverityTypes(NotificationSeverity severity)
    {
        // Arrange & Act
        var dto = new CreateUserNotificationDto
        {
            UserId = Guid.NewGuid(),
            Type = severity,
            Message = "Test message"
        };

        // Assert
        dto.Type.Should().Be(severity);
    }
}
