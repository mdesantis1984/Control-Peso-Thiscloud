using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Application.Tests.Services;

public sealed class WeightLogServiceTests : IDisposable
{
    private readonly DbContext _context;
    private readonly Mock<ILogger<WeightLogService>> _loggerMock;
    private readonly WeightLogService _service;

    public WeightLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new TestDbContext(options);
        _loggerMock = new Mock<ILogger<WeightLogService>>();
        _service = new WeightLogService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenWeightLogExists_ShouldReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        var weightLog = CreateWeightLogEntity(logId, userId, new DateOnly(2026, 2, 15), 75.5);
        _context.Set<WeightLogs>().Add(weightLog);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(logId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(logId);
        result.UserId.Should().Be(userId);
        result.Weight.Should().Be(75.5m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenWeightLogDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByUserAsync Tests

    [Fact]
    public async Task GetByUserAsync_WhenNoFilters_ShouldReturnAllUserLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 15), 75.5);
        var log2 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 16), 75.2);
        var log3 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 17), 74.8);
        
        _context.Set<WeightLogs>().AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        var filter = new WeightLogFilter { UserId = userId, Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetByUserAsync(userId, filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetByUserAsync_WithDateRangeFilter_ShouldReturnFilteredLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 10), 76.0);
        var log2 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 15), 75.5);
        var log3 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 20), 75.0);
        
        _context.Set<WeightLogs>().AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        var filter = new WeightLogFilter
        {
            UserId = userId,
            DateRange = new DateRange
            {
                StartDate = new DateOnly(2026, 2, 12),
                EndDate = new DateOnly(2026, 2, 18)
            },
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetByUserAsync(userId, filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Date.Should().Be(new DateOnly(2026, 2, 15));
    }

    [Fact]
    public async Task GetByUserAsync_WithDescendingSortFalse_ShouldReturnAscendingOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 17), 74.8);
        var log2 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 15), 75.5);
        var log3 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 16), 75.2);
        
        _context.Set<WeightLogs>().AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

        var filter = new WeightLogFilter { UserId = userId, Page = 1, PageSize = 10, Descending = false };

        // Act
        var result = await _service.GetByUserAsync(userId, filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Date.Should().Be(new DateOnly(2026, 2, 15));
        result.Items[1].Date.Should().Be(new DateOnly(2026, 2, 16));
        result.Items[2].Date.Should().Be(new DateOnly(2026, 2, 17));
    }

    [Fact]
    public async Task GetByUserAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (int i = 1; i <= 15; i++)
        {
            var log = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, i), 75.0 + i * 0.1);
            _context.Set<WeightLogs>().Add(log);
        }
        await _context.SaveChangesAsync();

        var filter = new WeightLogFilter { UserId = userId, Page = 2, PageSize = 5 };

        // Act
        var result = await _service.GetByUserAsync(userId, filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateWeightLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        CreateUser(userId);

        var dto = new CreateWeightLogDto
        {
            UserId = userId,
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(8, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "Morning weight"
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Weight.Should().Be(75.5m);
        result.Trend.Should().Be(WeightTrend.Neutral); // No previous weight
        
        var saved = await _context.Set<WeightLogs>().FirstOrDefaultAsync(w => w.Id == result.Id.ToString());
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenFirstLog_ShouldSetUserStartingWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        CreateUser(userId);

        var dto = new CreateWeightLogDto
        {
            UserId = userId,
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(8, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act
        await _service.CreateAsync(dto);

        // Assert
        var user = await _context.Set<Users>().FirstOrDefaultAsync(u => u.Id == userId.ToString());
        user!.StartingWeight.Should().Be(75.5);
    }

    [Fact]
    public async Task CreateAsync_WhenWeightIncreases_ShouldSetTrendUp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        CreateUser(userId);
        
        var previousLog = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 14), 75.0);
        _context.Set<WeightLogs>().Add(previousLog);
        await _context.SaveChangesAsync();

        var dto = new CreateWeightLogDto
        {
            UserId = userId,
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(8, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Trend.Should().Be(WeightTrend.Up);
    }

    [Fact]
    public async Task CreateAsync_WhenWeightDecreases_ShouldSetTrendDown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        CreateUser(userId);
        
        var previousLog = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 14), 75.5);
        _context.Set<WeightLogs>().Add(previousLog);
        await _context.SaveChangesAsync();

        var dto = new CreateWeightLogDto
        {
            UserId = userId,
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(8, 30),
            Weight = 75.0m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Trend.Should().Be(WeightTrend.Down);
    }

    [Fact]
    public async Task CreateAsync_WhenWeightChangeLessThan01Kg_ShouldSetTrendNeutral()
    {
        // Arrange
        var userId = Guid.NewGuid();
        CreateUser(userId);
        
        var previousLog = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 14), 75.0);
        _context.Set<WeightLogs>().Add(previousLog);
        await _context.SaveChangesAsync();

        var dto = new CreateWeightLogDto
        {
            UserId = userId,
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(8, 30),
            Weight = 75.05m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Trend.Should().Be(WeightTrend.Neutral);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateWeightLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        CreateUser(userId);
        
        var weightLog = CreateWeightLogEntity(logId, userId, new DateOnly(2026, 2, 15), 75.5);
        _context.Set<WeightLogs>().Add(weightLog);
        await _context.SaveChangesAsync();

        var dto = new UpdateWeightLogDto
        {
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(9, 0),
            Weight = 76.0m,
            DisplayUnit = WeightUnit.Kg,
            Note = "Updated weight"
        };

        // Act
        var result = await _service.UpdateAsync(logId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Weight.Should().Be(76.0m);
        result.Note.Should().Be("Updated weight");
    }

    [Fact]
    public async Task UpdateAsync_WhenWeightLogDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateWeightLogDto
        {
            Date = new DateOnly(2026, 2, 15),
            Time = new TimeOnly(9, 0),
            Weight = 76.0m,
            DisplayUnit = WeightUnit.Kg
        };

        // Act
        var act = async () => await _service.UpdateAsync(nonExistentId, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Weight log with ID {nonExistentId} not found.");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenWeightLogExists_ShouldDeleteSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logId = Guid.NewGuid();
        var weightLog = CreateWeightLogEntity(logId, userId, new DateOnly(2026, 2, 15), 75.5);
        _context.Set<WeightLogs>().Add(weightLog);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(logId);

        // Assert
        var deleted = await _context.Set<WeightLogs>().FirstOrDefaultAsync(w => w.Id == logId.ToString());
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenWeightLogDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _service.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Weight log with ID {nonExistentId} not found.");
    }

    #endregion

    #region GetStatsAsync Tests

    [Fact]
    public async Task GetStatsAsync_WithMultipleLogs_ShouldCalculateCorrectStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 10), 76.0);
        var log2 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 15), 75.5);
        var log3 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 20), 75.0);
        var log4 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 25), 74.5);

        _context.Set<WeightLogs>().AddRange(log1, log2, log3, log4);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 2, 28)
        };

        // Act
        var result = await _service.GetStatsAsync(userId, range);

        // Assert
        result.CurrentWeight.Should().Be(74.5m); // Last weight
        result.StartingWeight.Should().Be(76.0m); // First weight
        result.AverageWeight.Should().BeApproximately(75.25m, 0.01m);
        result.MinWeight.Should().Be(74.5m);
        result.MaxWeight.Should().Be(76.0m);
        result.TotalChange.Should().Be(-1.5m); // 74.5 - 76.0
        result.TotalRecords.Should().Be(4);
    }

    [Fact]
    public async Task GetStatsAsync_WhenNoLogsInRange_ShouldReturnEmptyStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 2, 28)
        };

        // Act
        var result = await _service.GetStatsAsync(userId, range);

        // Assert
        result.CurrentWeight.Should().BeNull();
        result.StartingWeight.Should().BeNull();
        result.AverageWeight.Should().BeNull();
        result.MinWeight.Should().BeNull();
        result.MaxWeight.Should().BeNull();
        result.TotalChange.Should().BeNull();
        result.TotalRecords.Should().Be(0);
    }

    [Fact]
    public async Task GetStatsAsync_WithDateRangeFilter_ShouldOnlyIncludeLogsInRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var log1 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 1, 31), 77.0); // Outside range
        var log2 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 10), 76.0);
        var log3 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 2, 20), 75.0);
        var log4 = CreateWeightLogEntity(Guid.NewGuid(), userId, new DateOnly(2026, 3, 1), 74.0); // Outside range
        
        _context.Set<WeightLogs>().AddRange(log1, log2, log3, log4);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 2, 28)
        };

        // Act
        var result = await _service.GetStatsAsync(userId, range);

        // Assert
        result.TotalRecords.Should().Be(2);
        result.StartingWeight.Should().Be(76.0m);
        result.CurrentWeight.Should().Be(75.0m);
    }

    #endregion

    #region Helper Methods

    private WeightLogs CreateWeightLogEntity(Guid id, Guid userId, DateOnly date, double weight)
    {
        return new WeightLogs
        {
            Id = id.ToString(),
            UserId = userId.ToString(),
            Date = date.ToString("yyyy-MM-dd"),
            Time = "08:00",
            Weight = weight,
            DisplayUnit = (int)WeightUnit.Kg,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private void CreateUser(Guid userId)
    {
        var user = new Users
        {
            Id = userId.ToString(),
            GoogleId = $"google_{userId}",
            Name = "Test User",
            Email = $"test_{userId}@example.com",
            Role = (int)UserRole.User,
            MemberSince = DateTime.UtcNow.ToString("O"),
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            Language = "es",
            Status = (int)UserStatus.Active,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
        
        _context.Set<Users>().Add(user);
        _context.SaveChanges();
    }

    #endregion

    // Test DbContext for in-memory testing
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure WeightLogs entity
            modelBuilder.Entity<WeightLogs>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Time).IsRequired();
                entity.Property(e => e.Weight).IsRequired();
                entity.Property(e => e.DisplayUnit).IsRequired();
                entity.Property(e => e.Trend).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configure Users entity
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.GoogleId).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.MemberSince).IsRequired();
                entity.Property(e => e.Height).IsRequired();
                entity.Property(e => e.UnitSystem).IsRequired();
                entity.Property(e => e.Language).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });
        }
    }
}
