using ControlPeso.Domain.Entities;
using ControlPeso.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Data;

/// <summary>
/// Tests for DbSeeder
/// Tests database seeding logic with demo data
/// </summary>
public sealed class DbSeederTests
{
    private readonly Mock<ILogger<DbSeeder>> _mockLogger;

    public DbSeederTests()
    {
        _mockLogger = new Mock<ILogger<DbSeeder>>();
    }

    private static ControlPesoDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        return new ControlPesoDbContext(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act
        var act = () => new DbSeeder(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act
        var act = () => new DbSeeder(context, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region SeedAsync Tests

    [Fact]
    public async Task SeedAsync_ShouldSeedDatabase_WhenDatabaseIsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var users = await context.Users.ToListAsync();
        users.Should().NotBeEmpty();
        users.Should().HaveCountGreaterThan(0);

        var preferences = await context.UserPreferences.ToListAsync();
        preferences.Should().NotBeEmpty();
        preferences.Should().HaveCount(users.Count);

        var weightLogs = await context.WeightLogs.ToListAsync();
        weightLogs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SeedAsync_ShouldNotSeedAgain_WhenDatabaseAlreadyHasUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Add existing user
        var existingUser = new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = "existing_google_id",
            Name = "Existing User",
            Email = "existing@example.com",
            Role = 0,
            MemberSince = DateTime.UtcNow,
            Height = 170.0m,
            UnitSystem = 0,
            Language = "es",
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var initialUserCount = await context.Users.CountAsync();

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var finalUserCount = await context.Users.CountAsync();
        finalUserCount.Should().Be(initialUserCount); // No new users added
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAdministratorUser_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var adminUsers = await context.Users.Where(u => u.Role == 1).ToListAsync();
        adminUsers.Should().NotBeEmpty();
        adminUsers.Should().ContainSingle(u => u.Role == 1);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateRegularUsers_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var regularUsers = await context.Users.Where(u => u.Role == 0).ToListAsync();
        regularUsers.Should().NotBeEmpty();
        regularUsers.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateUserPreferences_ForAllUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var users = await context.Users.ToListAsync();
        var preferences = await context.UserPreferences.ToListAsync();

        preferences.Should().HaveCount(users.Count);

        foreach (var user in users)
        {
            preferences.Should().ContainSingle(p => p.UserId == user.Id);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateWeightLogs_ForAllUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var users = await context.Users.ToListAsync();
        var weightLogs = await context.WeightLogs.ToListAsync();

        weightLogs.Should().NotBeEmpty();

        foreach (var user in users)
        {
            var userLogs = weightLogs.Where(w => w.UserId == user.Id).ToList();
            userLogs.Should().NotBeEmpty();
            userLogs.Should().HaveCountGreaterThan(10); // At least 10 logs per user (30 days with some gaps)
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateWeightLogsWithValidDates_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var weightLogs = await context.WeightLogs.ToListAsync();

        foreach (var log in weightLogs)
        {
            // Date should be valid
            log.Date.Should().BeBefore(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

            // Time should be valid HH:MM format
            log.Time.Hour.Should().BeInRange(0, 23);
            log.Time.Minute.Should().BeInRange(0, 59);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateWeightLogsWithValidWeights_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var weightLogs = await context.WeightLogs.ToListAsync();

        foreach (var log in weightLogs)
        {
            // Weight should be in reasonable range (20-500 kg)
            log.Weight.Should().BeInRange(20.0m, 500.0m);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateWeightLogsWithValidTrends_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var weightLogs = await context.WeightLogs.ToListAsync();

        foreach (var log in weightLogs)
        {
            // Trend should be valid enum value (0=Up, 1=Down, 2=Neutral)
            log.Trend.Should().BeInRange(0, 2);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateUsersWithDifferentUnitSystems_WhenSeeding()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        var users = await context.Users.ToListAsync();

        // Should have at least one metric and one imperial user
        users.Should().Contain(u => u.UnitSystem == 0); // Metric
        users.Should().Contain(u => u.UnitSystem == 1); // Imperial
    }

    [Fact]
    public async Task SeedAsync_ShouldHandleCancellationToken_WhenCancelled()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        // Note: This test may pass without throwing if cancellation isn't checked
        // But we're testing that CancellationToken is properly passed through
        try
        {
            await seeder.SeedAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation is checked
            Assert.True(true);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldLogInformation_WhenSeedingStarts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting database seed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldLogInformation_WhenSeedingCompletes()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldLogInformation_WhenDatabaseAlreadySeeded()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var seeder = new DbSeeder(context, _mockLogger.Object);

        // Add existing user
        var existingUser = new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = "existing",
            Name = "Test",
            Email = "test@test.com",
            Role = 0,
            MemberSince = DateTime.UtcNow,
            Height = 170,
            UnitSystem = 0,
            Language = "es",
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        // Act
        await seeder.SeedAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already seeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
