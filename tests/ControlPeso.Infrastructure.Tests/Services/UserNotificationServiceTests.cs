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
    private static (ControlPesoDbContext context, string dbName) CreateDbContext()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return (new ControlPesoDbContext(options), dbName);
    }

    private static UserNotificationService CreateService(
        string dbName,
        Mock<ILogger<UserNotificationService>>? loggerMock = null)
    {
        loggerMock ??= new Mock<ILogger<UserNotificationService>>();

        var dbOptions = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Crear factory mock que crea nuevas instancias apuntando a la misma DB
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContext())
            .Returns(() => new ControlPesoDbContext(dbOptions));
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken ct) => new ControlPesoDbContext(dbOptions));

        return new UserNotificationService(factoryMock.Object, loggerMock.Object);
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
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserNotificationService(factoryMock.Object, null!));
    }

    #endregion

    #region GetUnreadAsync Tests

    [Fact]
    public async Task GetUnreadAsync_WhenUnreadNotificationsExist_ShouldReturnUnreadNotifications()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            var notification1 = new UserNotifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = nameof(NotificationSeverity.Info),
                Title = "Test 1",
                Message = "Message 1",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var notification2 = new UserNotifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = nameof(NotificationSeverity.Warning),
                Title = "Test 2",
                Message = "Message 2",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            };

            context.UserNotifications.AddRange(notification1, notification2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUnreadAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.All(n => n.IsRead == false).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetUnreadAsync_WhenNoUnreadNotifications_ShouldReturnEmptyList()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            // Act
            var result = await service.GetUnreadAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetUnreadAsync_ShouldIgnoreReadNotifications()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            var unreadNotification = new UserNotifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = nameof(NotificationSeverity.Info),
                Title = "Unread",
                Message = "Message",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var readNotification = new UserNotifications
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = nameof(NotificationSeverity.Info),
                Title = "Read",
                Message = "Message",
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            context.UserNotifications.AddRange(unreadNotification, readNotification);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUnreadAsync(userId);

            // Assert
            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Unread");
        }
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            for (int i = 0; i < 25; i++)
            {
                var notification = new UserNotifications
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = nameof(NotificationSeverity.Info),
                    Title = $"Test {i}",
                    Message = "Message",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
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
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSecondPage()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            for (int i = 0; i < 25; i++)
            {
                var notification = new UserNotifications
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = nameof(NotificationSeverity.Info),
                    Title = $"Test {i}",
                    Message = "Message",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
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
    }

    [Fact]
    public async Task GetAllAsync_WhenNoNotifications_ShouldReturnEmptyResult()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            // Act
            var result = await service.GetAllAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            for (int i = 0; i < 5; i++)
            {
                var notification = new UserNotifications
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = nameof(NotificationSeverity.Info),
                    Title = $"Test {i}",
                    Message = "Message",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                };

                context.UserNotifications.Add(notification);
            }

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUnreadCountAsync(userId);

            // Assert
            result.Should().Be(5);
        }
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenNoUnreadNotifications_ShouldReturnZero()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            // Act
            var result = await service.GetUnreadCountAsync(userId);

            // Assert
            result.Should().Be(0);
        }
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldCreateNotification()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
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
                .FirstOrDefaultAsync(n => n.Id == result.Id);

            saved.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.CreateAsync(null!));
        }
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_ShouldMarkAsRead()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        var notificationId = Guid.NewGuid();

        using (context)
        {
            var notification = new UserNotifications
            {
                Id = notificationId,
                UserId = Guid.NewGuid(),
                Type = nameof(NotificationSeverity.Info),
                Title = "Test",
                Message = "Message",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserNotifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await service.MarkAsReadAsync(notificationId);
        }

        // Assert - crear nuevo contexto para verificar cambios
        using (var verifyContext = new ControlPesoDbContext(
            new DbContextOptionsBuilder<ControlPesoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var updated = await verifyContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            updated.Should().NotBeNull();
            updated!.IsRead.Should().BeTrue();
        }
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var notificationId = Guid.NewGuid();

            // Act & Assert
            await service.MarkAsReadAsync(notificationId); // Should not throw
        }
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenAlreadyRead_ShouldRemainRead()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        var notificationId = Guid.NewGuid();

        using (context)
        {
            var notification = new UserNotifications
            {
                Id = notificationId,
                UserId = Guid.NewGuid(),
                Type = nameof(NotificationSeverity.Info),
                Title = "Test",
                Message = "Message",
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            context.UserNotifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await service.MarkAsReadAsync(notificationId);
        }

        // Assert - crear nuevo contexto para verificar cambios
        using (var verifyContext = new ControlPesoDbContext(
            new DbContextOptionsBuilder<ControlPesoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var updated = await verifyContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            updated.Should().NotBeNull();
            updated!.IsRead.Should().BeTrue();
        }
    }

    #endregion

    #region MarkAllAsReadAsync Tests

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUnreadNotifications()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        var userId = Guid.NewGuid();

        using (context)
        {
            for (int i = 0; i < 3; i++)
            {
                var notification = new UserNotifications
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = nameof(NotificationSeverity.Info),
                    Title = $"Test {i}",
                    Message = "Message",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                };

                context.UserNotifications.Add(notification);
            }

            await context.SaveChangesAsync();

            // Act
            await service.MarkAllAsReadAsync(userId);
        }

        // Assert - crear nuevo contexto para verificar cambios
        using (var verifyContext = new ControlPesoDbContext(
            new DbContextOptionsBuilder<ControlPesoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var allNotifications = await verifyContext.UserNotifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            allNotifications.Should().HaveCount(3);
            allNotifications.All(n => n.IsRead).Should().BeTrue();
        }
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoUnreadNotifications_ShouldNotThrow()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            // Act & Assert
            await service.MarkAllAsReadAsync(userId); // Should not throw
        }
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenNotificationExists_ShouldDeleteNotification()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        var notificationId = Guid.NewGuid();

        using (context)
        {
            var notification = new UserNotifications
            {
                Id = notificationId,
                UserId = Guid.NewGuid(),
                Type = nameof(NotificationSeverity.Info),
                Title = "Test",
                Message = "Message",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            context.UserNotifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            await service.DeleteAsync(notificationId);
        }

        // Assert - crear nuevo contexto para verificar cambios
        using (var verifyContext = new ControlPesoDbContext(
            new DbContextOptionsBuilder<ControlPesoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var deleted = await verifyContext.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            deleted.Should().BeNull();
        }
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var notificationId = Guid.NewGuid();

            // Act & Assert
            await service.DeleteAsync(notificationId); // Should not throw
        }
    }

    #endregion

    #region DeleteAllAsync Tests

    [Fact]
    public async Task DeleteAllAsync_ShouldDeleteAllNotifications()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        var userId = Guid.NewGuid();

        using (context)
        {
            for (int i = 0; i < 5; i++)
            {
                var notification = new UserNotifications
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = nameof(NotificationSeverity.Info),
                    Title = $"Test {i}",
                    Message = "Message",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                };

                context.UserNotifications.Add(notification);
            }

            await context.SaveChangesAsync();

            // Act
            await service.DeleteAllAsync(userId);
        }

        // Assert - crear nuevo contexto para verificar cambios
        using (var verifyContext = new ControlPesoDbContext(
            new DbContextOptionsBuilder<ControlPesoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options))
        {
            var remaining = await verifyContext.UserNotifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            remaining.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task DeleteAllAsync_WhenNoNotifications_ShouldNotThrow()
    {
        // Arrange
        var (context, dbName) = CreateDbContext();
        var service = CreateService(dbName);
        using (context)
        {
            var userId = Guid.NewGuid();

            // Act & Assert
            await service.DeleteAllAsync(userId); // Should not throw
        }
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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

        var loggerMock = new Mock<ILogger<UserNotificationService>>();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new ControlPesoDbContext(options);
                ctx.Dispose();
                return ctx;
            });

        var service = new UserNotificationService(factoryMock.Object, loggerMock.Object);
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
