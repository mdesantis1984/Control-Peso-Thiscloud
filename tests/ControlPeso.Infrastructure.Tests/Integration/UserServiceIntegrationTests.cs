using ControlPeso.Application.DTOs;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Enums;
using ControlPeso.Infrastructure.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlPeso.Infrastructure.Tests.Integration;

/// <summary>
/// Tests de integración E2E para UserService.
/// </summary>
public sealed class UserServiceIntegrationTests : IDisposable
{
    private ControlPesoDbContext? _context;
    private SqliteConnection? _connection;

    public void Dispose()
    {
        // Disponer en orden correcto: contexto primero, luego conexión
        _context?.Dispose();
        _connection?.Dispose();
        // No usar EnsureDeleted() para in-memory SQLite - se borra automáticamente al cerrar conexión
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = await InMemoryDbContextFactory.CreateWithSeedDataAsync(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var userId = Guid.Parse(existingUser.Id);

        // Act
        var result = await service.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(existingUser.Name, result.Name);
        Assert.Equal(existingUser.Email, result.Email);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WithExistingGoogleId_ShouldReturnUser()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = await InMemoryDbContextFactory.CreateWithSeedDataAsync(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var existingUser = await _context.Users.FirstAsync();

        // Act
        var result = await service.GetByGoogleIdAsync(existingUser.GoogleId!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser.GoogleId, result.GoogleId);
        Assert.Equal(existingUser.Email, result.Email);
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNewUser_ShouldCreateUser()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = InMemoryDbContextFactory.Create(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var googleInfo = new GoogleUserInfo
        {
            GoogleId = "google_new_12345",
            Name = "New Google User",
            Email = "newuser@gmail.com",
            AvatarUrl = "https://lh3.googleusercontent.com/a/test123"
        };

        // Act
        var result = await service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("google_new_12345", result.GoogleId);
        Assert.Equal("New Google User", result.Name);
        Assert.Equal("newuser@gmail.com", result.Email);
        Assert.Equal(UserRole.User, result.Role);
        Assert.Equal(UserStatus.Active, result.Status);

        // Verify persistence
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == "google_new_12345");
        Assert.NotNull(savedUser);
        Assert.Equal("New Google User", savedUser.Name);
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithExistingUser_ShouldUpdateUser()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = await InMemoryDbContextFactory.CreateWithSeedDataAsync(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var originalName = existingUser.Name;
        var originalAvatarUrl = existingUser.AvatarUrl;

        // IMPORTANTE: Usar valores DIFERENTES a los existentes para forzar actualización
        // UpdateFromGoogle solo actualiza si el valor cambia (línea 112 de UserMapper)
        var updatedName = $"{originalName} - Updated {DateTime.UtcNow.Ticks}";
        var updatedAvatarUrl = $"https://new-avatar-url.com/updated-{Guid.NewGuid():N}.jpg";

        var googleInfo = new GoogleUserInfo
        {
            GoogleId = existingUser.GoogleId!,
            Name = updatedName,
            Email = existingUser.Email,
            AvatarUrl = updatedAvatarUrl
        };

        // Act
        var result = await service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser.GoogleId, result.GoogleId);
        Assert.Equal(updatedName, result.Name);
        Assert.Equal(updatedAvatarUrl, result.AvatarUrl);

        // Verify update persisted
        var updatedUser = await _context.Users.FindAsync(existingUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(updatedName, updatedUser.Name);
        Assert.NotEqual(originalName, updatedUser.Name);
        Assert.Equal(updatedAvatarUrl, updatedUser.AvatarUrl);
        Assert.NotEqual(originalAvatarUrl, updatedUser.AvatarUrl);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateUserProfile()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = await InMemoryDbContextFactory.CreateWithSeedDataAsync(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var userId = Guid.Parse(existingUser.Id);

        var updateDto = new UpdateUserProfileDto
        {
            Name = existingUser.Name,
            Height = 180.0m,
            GoalWeight = 75.0m,
            UnitSystem = UnitSystem.Imperial,
            Language = "en"
        };

        // Act
        var result = await service.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(180.0m, result.Height);
        Assert.Equal(75.0m, result.GoalWeight);
        Assert.Equal(UnitSystem.Imperial, result.UnitSystem);
        Assert.Equal("en", result.Language);

        // Verify persistence
        var updatedUser = await _context.Users.FindAsync(existingUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(180.0, updatedUser.Height);  // DB stores as double
        Assert.Equal(75.0, updatedUser.GoalWeight);  // DB stores as double
        Assert.Equal((int)UnitSystem.Imperial, updatedUser.UnitSystem);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = InMemoryDbContextFactory.Create(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WithNonExistentGoogleId_ShouldReturnNull()
    {
        // Arrange
        var databaseName = $"UserServiceTests_{Guid.NewGuid()}";
        (_context, _connection) = InMemoryDbContextFactory.Create(databaseName);
        var service = new UserService(_context, NullLogger<UserService>.Instance);

        // Act
        var result = await service.GetByGoogleIdAsync("google_nonexistent_999");

        // Assert
        Assert.Null(result);
    }
}
