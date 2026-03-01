using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Entities;
using ControlPeso.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Services;

/// <summary>
/// Tests para UserPreferencesService.
/// Usa SQLite in-memory con conexión compartida mantenida abierta durante cada test.
/// </summary>
public sealed class UserPreferencesServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ControlPesoDbContext> _options;
    private readonly Mock<ILogger<UserPreferencesService>> _loggerMock;

    public UserPreferencesServiceTests()
    {
        // Crear conexión SQLite en memoria que se mantiene abierta durante el test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Crear schema inicial
        using var context = new ControlPesoDbContext(_options);
        context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<UserPreferencesService>>();
    }

    private IDbContextFactory<ControlPesoDbContext> CreateFactory()
    {
        // Factory que usa las mismas opciones (conexión compartida)
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContext())
            .Returns(() => new ControlPesoDbContext(_options));
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken ct) => new ControlPesoDbContext(_options));

        return factoryMock.Object;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var userId = Guid.NewGuid();

        await using var context = new ControlPesoDbContext(_options);
        context.Users.Add(new Users
        {
            Id = userId,
            GoogleId = $"google_{userId}",
            Name = "Test User",
            Email = $"test_{userId}@example.com",
            Role = 0,
            Height = 170.0m,
            UnitSystem = 0,
            Language = "es",
            Status = 0,
            MemberSince = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public void Constructor_WhenFactoryIsNull_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserPreferencesService(null!, _loggerMock.Object));

        Assert.Equal("contextFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserPreferencesService(factory, null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenNoPreferencesExist_ReturnsTrue()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        Assert.True(result);

        // Verificar que se crearon preferencias por defecto
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.True(preferences.DarkMode);
    }

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenPreferencesExist_ReturnsStoredValue()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Crear preferencias con DarkMode = false
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = false,
                NotificationsEnabled = true,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenNoPreferencesExist_ReturnsTrue()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        Assert.True(result);

        // Verificar que se crearon preferencias por defecto
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.True(preferences.NotificationsEnabled);
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenPreferencesExist_ReturnsStoredValue()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Crear preferencias con NotificationsEnabled = false
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true,
                NotificationsEnabled = false,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WhenPreferencesExist_UpdatesValue()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Crear preferencias iniciales
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true,
                NotificationsEnabled = true,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert
        await using var verifyContext = new ControlPesoDbContext(_options);
        var preferences = await verifyContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode);
        Assert.True(preferences.NotificationsEnabled); // No debe cambiar
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WhenNoPreferencesExist_CreatesPreferencesAndUpdates()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert - verificar que se crearon preferencias
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode); // Valor actualizado
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenPreferencesExist_UpdatesValue()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Crear preferencias iniciales
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true,
                NotificationsEnabled = true,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdateNotificationsEnabledAsync(userId, false);

        // Assert
        await using var verifyContext = new ControlPesoDbContext(_options);
        var preferences = await verifyContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.NotificationsEnabled);
        Assert.True(preferences.DarkMode); // No debe cambiar
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenNoPreferencesExist_CreatesPreferencesAndUpdates()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdateNotificationsEnabledAsync(userId, false);

        // Assert
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.NotificationsEnabled); // Valor actualizado
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenPreferencesExist_UpdatesBothValues()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Crear preferencias iniciales
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true,
                NotificationsEnabled = true,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdatePreferencesAsync(userId, isDarkMode: false, notificationsEnabled: false);

        // Assert
        await using var verifyContext = new ControlPesoDbContext(_options);
        var preferences = await verifyContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode);
        Assert.False(preferences.NotificationsEnabled);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenNoPreferencesExist_CreatesPreferencesWithValues()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdatePreferencesAsync(userId, isDarkMode: false, notificationsEnabled: false);

        // Assert
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode);
        Assert.False(preferences.NotificationsEnabled);
    }

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenNoPreferencesExist_CreatesDefaults()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.CreateDefaultPreferencesAsync(userId);

        // Assert
        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.True(preferences.DarkMode);
        Assert.True(preferences.NotificationsEnabled);
        Assert.Equal("America/Argentina/Buenos_Aires", preferences.TimeZone);
    }

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenPreferencesAlreadyExist_DoesNothing()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var originalUpdatedAt = DateTime.UtcNow.AddDays(-5);

        // Crear preferencias existentes
        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = false,
                NotificationsEnabled = false,
                TimeZone = "Europe/London",
                UpdatedAt = originalUpdatedAt
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.CreateDefaultPreferencesAsync(userId);

        // Assert - preferencias no deben cambiar
        await using var verifyContext = new ControlPesoDbContext(_options);
        var preferences = await verifyContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode); // Mantiene valor original
        Assert.False(preferences.NotificationsEnabled); // Mantiene valor original
        Assert.Equal("Europe/London", preferences.TimeZone); // Mantiene valor original

        // Verificar que solo hay una preferencia
        var count = await verifyContext.UserPreferences
            .CountAsync(p => p.UserId == userId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_UpdatesTimestamp()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);

        await using (var context = new ControlPesoDbContext(_options))
        {
            context.UserPreferences.Add(new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DarkMode = true,
                NotificationsEnabled = true,
                TimeZone = "America/Argentina/Buenos_Aires",
                UpdatedAt = originalUpdatedAt
            });
            await context.SaveChangesAsync();
        }

        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        // Act
        await service.UpdateDarkModeAsync(userId, false);

        // Assert
        await using var verifyContext = new ControlPesoDbContext(_options);
        var preferences = await verifyContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task GetDarkModePreferenceAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId, cts.Token);

        // Assert
        Assert.True(result);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId, cts.Token);

        // Assert
        Assert.True(result);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var factory = CreateFactory();
        var service = new UserPreferencesService(factory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        await service.UpdateDarkModeAsync(userId, false, cts.Token);

        // Assert
        Assert.False(cts.Token.IsCancellationRequested);

        await using var context = new ControlPesoDbContext(_options);
        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(preferences);
        Assert.False(preferences.DarkMode);
    }

    #region Database Error Scenarios

    [Fact]
    public async Task GetDarkModePreferenceAsync_WhenDatabaseError_ReturnsTrueAndLogsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetDarkModePreferenceAsync(userId);

        // Assert
        Assert.True(result); // Should return default true on error

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNotificationsEnabledAsync_WhenDatabaseError_ReturnsTrueAndLogsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetNotificationsEnabledAsync(userId);

        // Assert
        Assert.True(result); // Should return default true on error

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDarkModeAsync_WhenDatabaseError_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateDarkModeAsync(userId, false));
    }

    [Fact]
    public async Task UpdateNotificationsEnabledAsync_WhenDatabaseError_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateNotificationsEnabledAsync(userId, false));
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WhenDatabaseError_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdatePreferencesAsync(userId, false, false));
    }

    [Fact]
    public async Task CreateDefaultPreferencesAsync_WhenDatabaseError_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var factoryMock = new Mock<IDbContextFactory<ControlPesoDbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserPreferencesService(factoryMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateDefaultPreferencesAsync(userId));
    }

    #endregion

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
