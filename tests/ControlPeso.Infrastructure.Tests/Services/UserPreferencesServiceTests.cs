using ControlPeso.Domain.Entities;
using ControlPeso.Infrastructure.Data;
using ControlPeso.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Services;

/// <summary>
/// Tests para UserPreferencesService
/// </summary>
public class UserPreferencesServiceTests
{
    private static ControlPesoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        return new ControlPesoDbContext(options);
    }

    private static UserPreferencesService CreateService(
        ControlPesoDbContext context,
        Mock<ILogger<UserPreferencesService>>? loggerMock = null)
    {
        loggerMock ??= new Mock<ILogger<UserPreferencesService>>();
        return new UserPreferencesService(context, loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UserPreferencesService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserPreferencesService(null!, loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserPreferencesService(context, null!));
    }

    #endregion

    #region GetDarkModePreferenceAsync Tests

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenPreferencesExist_ShouldReturnDarkModeValue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenDarkModeDisabled_ShouldReturnFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 0,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenPreferencesDoNotExist_ShouldCreateDefaultsAndReturnTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        result.Should().BeTrue(); // Dark mode por defecto

        // Verificar que se crearon las preferencias por defecto
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DarkMode.Should().Be(1);
    }

    #endregion

    #region GetNotificationsEnabledAsync Tests

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenPreferencesExist_ShouldReturnNotificationsValue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenNotificationsDisabled_ShouldReturnFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 0,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenPreferencesDoNotExist_ShouldCreateDefaultsAndReturnTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        result.Should().BeTrue(); // Notificaciones habilitadas por defecto

        // Verificar que se crearon las preferencias por defecto
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.NotificationsEnabled.Should().Be(1);
    }

    #endregion

    #region UpdateDarkModeAsync Tests

    [Fact]
    public async Task UpdateDarkModeAsync_WhenPreferencesExist_ShouldUpdateDarkMode()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.DarkMode.Should().Be(0);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WhenPreferencesDoNotExist_ShouldCreateAndUpdate()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DarkMode.Should().Be(0);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var oldTimestamp = DateTime.UtcNow.AddHours(-1).ToString("O");
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = oldTimestamp
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.UpdatedAt.Should().NotBe(oldTimestamp);
    }

    #endregion

    #region UpdateNotificationsEnabledAsync Tests

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenPreferencesExist_ShouldUpdateNotifications()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateNotificationsEnabledAsync(userId, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.NotificationsEnabled.Should().Be(0);
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenPreferencesDoNotExist_ShouldCreateAndUpdate()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        await service.UpdateNotificationsEnabledAsync(userId, false);

        // Assert
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.NotificationsEnabled.Should().Be(0);
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var oldTimestamp = DateTime.UtcNow.AddHours(-1).ToString("O");
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = oldTimestamp
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateNotificationsEnabledAsync(userId, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.UpdatedAt.Should().NotBe(oldTimestamp);
    }

    #endregion

    #region UpdatePreferencesAsync Tests

    [Fact]
    public async Task UpdatePreferencesAsync_WhenPreferencesExist_ShouldUpdateBothValues()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdatePreferencesAsync(userId, false, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.DarkMode.Should().Be(0);
        updatedPreferences.NotificationsEnabled.Should().Be(0);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenPreferencesDoNotExist_ShouldCreateAndUpdate()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        await service.UpdatePreferencesAsync(userId, false, false);

        // Assert
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DarkMode.Should().Be(0);
        preferences.NotificationsEnabled.Should().Be(0);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var oldTimestamp = DateTime.UtcNow.AddHours(-1).ToString("O");
        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 1,
            NotificationsEnabled = 1,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = oldTimestamp
        };

        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        // Act
        await service.UpdatePreferencesAsync(userId, false, false);

        // Assert
        var updatedPreferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.UpdatedAt.Should().NotBe(oldTimestamp);
    }

    #endregion

    #region CreateDefaultPreferencesAsync Tests

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenPreferencesDoNotExist_ShouldCreateDefaults()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        await service.CreateDefaultPreferencesAsync(userId);

        // Assert
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DarkMode.Should().Be(1); // Dark mode por defecto
        preferences.NotificationsEnabled.Should().Be(1); // Notificaciones habilitadas por defecto
        preferences.TimeZone.Should().Be("America/Argentina/Buenos_Aires");
    }

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenPreferencesAlreadyExist_ShouldNotCreateDuplicates()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var existingPreferences = new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            DarkMode = 0,
            NotificationsEnabled = 0,
            TimeZone = "America/Argentina/Buenos_Aires",
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        context.UserPreferences.Add(existingPreferences);
        await context.SaveChangesAsync();

        // Act
        await service.CreateDefaultPreferencesAsync(userId);

        // Assert
        var preferencesCount = await context.UserPreferences
            .CountAsync(p => p.UserId == userId.ToString());

        preferencesCount.Should().Be(1); // No se deben crear duplicados

        // Verificar que los valores originales se mantienen
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

        preferences.Should().NotBeNull();
        preferences!.DarkMode.Should().Be(0); // Valor original
        preferences.NotificationsEnabled.Should().Be(0); // Valor original
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenDatabaseThrowsException_ShouldReturnTrueAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception on queries

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        result.Should().BeTrue(); // Default fallback
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ERROR retrieving dark mode preference")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenDatabaseThrowsException_ShouldReturnTrueAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception on queries

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        result.Should().BeTrue(); // Default fallback
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ERROR retrieving notifications preference")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.UpdateDarkModeAsync(userId, true);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating dark mode preference")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.UpdateNotificationsEnabledAsync(userId, true);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating notifications preference")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.UpdatePreferencesAsync(userId, true, true);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating preferences")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenDatabaseThrowsException_ShouldThrowAndLogError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        using var context = new ControlPesoDbContext(options);
        context.Dispose(); // Dispose context to cause exception

        var loggerMock = new Mock<ILogger<UserPreferencesService>>();
        var service = new UserPreferencesService(context, loggerMock.Object);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.CreateDefaultPreferencesAsync(userId);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating default preferences")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
