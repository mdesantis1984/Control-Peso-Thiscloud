using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using ControlPeso.Infrastructure.Data;
using ControlPeso.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Services;

/// <summary>
/// Tests para UserNotificationService
/// </summary>
public class UserNotificationServiceTests
{
    private static ControlPesoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        return new ControlPesoDbContext(options);
    }

    private static UserNotificationService CreateService(
        ControlPesoDbContext context,
        Mock<ILogger<UserNotificationService>>? loggerMock = null)
    {
        loggerMock ??= new Mock<ILogger<UserNotificationService>>();
        return new UserNotificationService(context, loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UserNotificationService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserNotificationService(null!, loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserNotificationService(context, null!));
    }

    #endregion

    #region GetUnreadAsync Tests

    [Fact]
    public async Task GetUnreadAsync_WhenUnreadNotificationsExist_ShouldReturnUnreadNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var notification1 = new UserNotifications
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Test 1",
            Message = "Message 1",
            IsRead = 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = null
        };

        var notification2 = new UserNotifications
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Type = (int)NotificationSeverity.Warning,
            Title = "Test 2",
            Message = "Message 2",
            IsRead = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(1).ToString("O"),
            ReadAt = null
        };

        context.UserNotifications.AddRange(notification1, notification2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUnreadAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.All(n => n.IsRead == false).Should().BeTrue();
    }

    [Fact]
    public async Task GetUnreadAsync_WhenNoUnreadNotifications_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetUnreadAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnreadAsync_ShouldIgnoreReadNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var unreadNotification = new UserNotifications
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Unread",
            Message = "Message",
            IsRead = 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = null
        };

        var readNotification = new UserNotifications
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Read",
            Message = "Message",
            IsRead = 1,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = DateTime.UtcNow.ToString("O")
        };

        context.UserNotifications.AddRange(unreadNotification, readNotification);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUnreadAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Unread");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResults()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 25; i++)
        {
            var notification = new UserNotifications
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Type = (int)NotificationSeverity.Info,
                Title = $"Test {i}",
                Message = "Message",
                IsRead = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i).ToString("O"),
                ReadAt = null
            };

            context.UserNotifications.Add(notification);
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(userId, page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSecondPage()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 25; i++)
        {
            var notification = new UserNotifications
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Type = (int)NotificationSeverity.Info,
                Title = $"Test {i}",
                Message = "Message",
                IsRead = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i).ToString("O"),
                ReadAt = null
            };

            context.UserNotifications.Add(notification);
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync(userId, page: 2, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var notification = new UserNotifications
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Type = (int)NotificationSeverity.Info,
                Title = $"Test {i}",
                Message = "Message",
                IsRead = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i).ToString("O"),
                ReadAt = null
            };

            context.UserNotifications.Add(notification);
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenNoUnreadNotifications_ShouldReturnZero()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreateNotification()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var dto = new CreateUserNotificationDto
        {
            UserId = Guid.NewGuid(),
            Type = NotificationSeverity.Info,
            Title = "Test Notification",
            Message = "Test Message"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(dto.UserId);
        result.Type.Should().Be(dto.Type);
        result.Title.Should().Be(dto.Title);
        result.Message.Should().Be(dto.Message);
        result.IsRead.Should().BeFalse();

        // Verificar que se guardó en DB
        var saved = await context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == result.Id.ToString());

        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.CreateAsync(null!));
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_ShouldMarkAsRead()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var notificationId = Guid.NewGuid();

        var notification = new UserNotifications
        {
            Id = notificationId.ToString(),
            UserId = Guid.NewGuid().ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Test",
            Message = "Message",
            IsRead = 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = null
        };

        context.UserNotifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        await service.MarkAsReadAsync(notificationId);

        // Assert
        var updated = await context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId.ToString());

        updated.Should().NotBeNull();
        updated!.IsRead.Should().Be(1);
        updated.ReadAt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var notificationId = Guid.NewGuid();

        // Act & Assert
        await service.MarkAsReadAsync(notificationId); // Should not throw
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenAlreadyRead_ShouldNotChangeReadAt()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var notificationId = Guid.NewGuid();
        var originalReadAt = DateTime.UtcNow.AddHours(-1).ToString("O");

        var notification = new UserNotifications
        {
            Id = notificationId.ToString(),
            UserId = Guid.NewGuid().ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Test",
            Message = "Message",
            IsRead = 1,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = originalReadAt
        };

        context.UserNotifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        await service.MarkAsReadAsync(notificationId);

        // Assert
        var updated = await context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId.ToString());

        updated.Should().NotBeNull();
        updated!.ReadAt.Should().Be(originalReadAt);
    }

    #endregion

    #region MarkAllAsReadAsync Tests

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUnreadNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 3; i++)
        {
            var notification = new UserNotifications
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Type = (int)NotificationSeverity.Info,
                Title = $"Test {i}",
                Message = "Message",
                IsRead = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i).ToString("O"),
                ReadAt = null
            };

            context.UserNotifications.Add(notification);
        }

        await context.SaveChangesAsync();

        // Act
        await service.MarkAllAsReadAsync(userId);

        // Assert
        var allNotifications = await context.UserNotifications
            .Where(n => n.UserId == userId.ToString())
            .ToListAsync();

        allNotifications.Should().HaveCount(3);
        allNotifications.All(n => n.IsRead == 1).Should().BeTrue();
        allNotifications.All(n => !string.IsNullOrWhiteSpace(n.ReadAt)).Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoUnreadNotifications_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act & Assert
        await service.MarkAllAsReadAsync(userId); // Should not throw
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenNotificationExists_ShouldDeleteNotification()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var notificationId = Guid.NewGuid();

        var notification = new UserNotifications
        {
            Id = notificationId.ToString(),
            UserId = Guid.NewGuid().ToString(),
            Type = (int)NotificationSeverity.Info,
            Title = "Test",
            Message = "Message",
            IsRead = 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ReadAt = null
        };

        context.UserNotifications.Add(notification);
        await context.SaveChangesAsync();

        // Act
        await service.DeleteAsync(notificationId);

        // Assert
        var deleted = await context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId.ToString());

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var notificationId = Guid.NewGuid();

        // Act & Assert
        await service.DeleteAsync(notificationId); // Should not throw
    }

    #endregion

    #region DeleteAllAsync Tests

    [Fact]
    public async Task DeleteAllAsync_ShouldDeleteAllNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var notification = new UserNotifications
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Type = (int)NotificationSeverity.Info,
                Title = $"Test {i}",
                Message = "Message",
                IsRead = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(i).ToString("O"),
                ReadAt = null
            };

            context.UserNotifications.Add(notification);
        }

        await context.SaveChangesAsync();

        // Act
        await service.DeleteAllAsync(userId);

        // Assert
        var remaining = await context.UserNotifications
            .Where(n => n.UserId == userId.ToString())
            .ToListAsync();

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllAsync_WhenNoNotifications_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act & Assert
        await service.DeleteAllAsync(userId); // Should not throw
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetUnreadAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.GetUnreadAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting unread notifications")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.GetAllAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting notifications")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.GetUnreadCountAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting unread count")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var dto = new CreateUserNotificationDto
        {
            UserId = Guid.NewGuid(),
            Type = NotificationSeverity.Info,
            Title = "Test",
            Message = "Message"
        };

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var notificationId = Guid.NewGuid();

        // Act
        var act = async () => await service.MarkAsReadAsync(notificationId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error marking notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.MarkAllAsReadAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error marking all notifications as read")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var notificationId = Guid.NewGuid();

        // Act
        var act = async () => await service.DeleteAsync(notificationId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAllAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var service = new UserNotificationService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.DeleteAllAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting all notifications")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
