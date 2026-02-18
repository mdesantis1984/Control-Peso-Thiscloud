using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Enums;
using ControlPeso.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlPeso.Infrastructure.Tests.Integration;

/// <summary>
/// Tests de integración E2E para WeightLogService.
/// Verifica que la stack completa Application → Infrastructure → InMemory DB funcione correctamente.
/// </summary>
public sealed class WeightLogServiceIntegrationTests : IDisposable
{
    private readonly string _databaseName = $"WeightLogServiceTests_{Guid.NewGuid()}";
    private ControlPesoDbContext? _context;

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var createDto = new CreateWeightLogDto
        {
            UserId = Guid.Parse(existingUser.Id),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(8, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "Test weight log from integration test"
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(createDto.Weight, result.Weight);
        Assert.Equal(createDto.Note, result.Note);

        // Verify persistence: query DB directly
        var savedLog = await _context.WeightLogs.FindAsync(result.Id.ToString());
        Assert.NotNull(savedLog);
        Assert.Equal(75.5, savedLog.Weight, precision: 2);
        Assert.Equal("Test weight log from integration test", savedLog.Note);
    }

    [Fact]
    public async Task GetByUserAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var userId = Guid.Parse(existingUser.Id);

        var filter = new WeightLogFilter
        {
            UserId = userId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetByUserAsync(userId, filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.True(result.Items.Count <= 10);
        Assert.Equal(1, result.Page);
        Assert.True(result.TotalPages >= 1);

        // Verify all items belong to the requested user
        Assert.All(result.Items, item => Assert.Equal(userId, item.UserId));
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRangeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var userId = Guid.Parse(existingUser.Id);

        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        var filter = new WeightLogFilter
        {
            UserId = userId,
            DateRange = new DateRange
            {
                StartDate = startDate,
                EndDate = endDate
            },
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await service.GetByUserAsync(userId, filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.Date >= startDate);
            Assert.True(item.Date <= endDate);
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingRecord()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        // Get existing weight log
        var existingLog = await _context.WeightLogs.FirstAsync();
        var logId = Guid.Parse(existingLog.Id);

        var updateDto = new UpdateWeightLogDto
        {
            Date = DateOnly.Parse(existingLog.Date),
            Time = TimeOnly.Parse(existingLog.Time),
            Weight = 80.0m,
            DisplayUnit = (WeightUnit)existingLog.DisplayUnit,
            Note = "Updated note from integration test"
        };

        // Act
        var result = await service.UpdateAsync(logId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(logId, result.Id);
        Assert.Equal(80.0m, result.Weight);
        Assert.Equal("Updated note from integration test", result.Note);

        // Verify persistence
        var updatedLog = await _context.WeightLogs.FindAsync(logId.ToString());
        Assert.NotNull(updatedLog);
        Assert.Equal(80.0, updatedLog.Weight, precision: 2);
        Assert.Equal("Updated note from integration test", updatedLog.Note);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRecord()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var existingLog = await _context.WeightLogs.FirstAsync();
        var logId = Guid.Parse(existingLog.Id);

        // Act
        await service.DeleteAsync(logId);

        // Assert - verify record is deleted
        var deletedLog = await _context.WeightLogs.FindAsync(logId.ToString());
        Assert.Null(deletedLog);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCorrectStatistics()
    {
        // Arrange
        _context = await InMemoryDbContextFactory.CreateWithSeedDataAsync(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var existingUser = await _context.Users.FirstAsync();
        var userId = Guid.Parse(existingUser.Id);

        var range = new DateRange
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var stats = await service.GetStatsAsync(userId, range);

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalRecords > 0);

        if (stats.AverageWeight.HasValue && stats.MinWeight.HasValue && stats.MaxWeight.HasValue)
        {
            Assert.True(stats.AverageWeight.Value > 0);
            Assert.True(stats.MinWeight.Value > 0);
            Assert.True(stats.MaxWeight.Value > 0);
            Assert.True(stats.MinWeight.Value <= stats.AverageWeight.Value);
            Assert.True(stats.AverageWeight.Value <= stats.MaxWeight.Value);
        }
    }

    [Fact]
    public async Task CreateAsync_WithFirstWeightLog_ShouldSetUserStartingWeight()
    {
        // Arrange
        _context = InMemoryDbContextFactory.Create(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        // Create a user WITHOUT starting weight
        var newUser = new Domain.Entities.Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "test_google_123",
            Name = "Test User",
            Email = "test@test.com",
            Role = (int)UserRole.User,
            MemberSince = DateTime.UtcNow.ToString("O"),
            Height = 175.0,
            UnitSystem = (int)UnitSystem.Metric,
            Language = "es",
            Status = (int)UserStatus.Active,
            StartingWeight = null, // NO starting weight yet
            GoalWeight = 70.0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var createDto = new CreateWeightLogDto
        {
            UserId = Guid.Parse(newUser.Id),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(8, 0),
            Weight = 85.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "First weight log"
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);

        // Verify StartingWeight was auto-set
        var updatedUser = await _context.Users.FindAsync(newUser.Id);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.StartingWeight);
        Assert.Equal(85.5, updatedUser.StartingWeight.Value, precision: 2);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        _context = InMemoryDbContextFactory.Create(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _context = InMemoryDbContextFactory.Create(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(8, 0),
            Weight = 80.0m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(nonExistentId, updateDto));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _context = InMemoryDbContextFactory.Create(_databaseName);
        var service = new WeightLogService(_context, NullLogger<WeightLogService>.Instance);

        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(nonExistentId));
    }
}
