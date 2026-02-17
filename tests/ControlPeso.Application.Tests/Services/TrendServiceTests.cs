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

public sealed class TrendServiceTests : IDisposable
{
    private readonly DbContext _context;
    private readonly Mock<ILogger<TrendService>> _loggerMock;
    private readonly TrendService _service;

    public TrendServiceTests()
    {
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new TestDbContext(options);
        _loggerMock = new Mock<ILogger<TrendService>>();
        _service = new TrendService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetTrendAnalysisAsync Tests

    [Fact]
    public async Task GetTrendAnalysisAsync_WithNoLogs_ShouldReturnEmptyAnalysis()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.OverallTrend.Should().Be(WeightTrend.Neutral);
        result.DataPoints.Should().BeEmpty();
        result.AverageDailyChange.Should().BeNull();
        result.AverageWeeklyChange.Should().BeNull();
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithUpwardTrend_ShouldReturnUpTrend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia al alza (70kg → 72kg en 10 días)
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, new DateOnly(2026, 1, 1), 70.0),
            CreateWeightLog(userId, new DateOnly(2026, 1, 5), 71.0),
            CreateWeightLog(userId, new DateOnly(2026, 1, 10), 72.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.OverallTrend.Should().Be(WeightTrend.Up);
        result.DataPoints.Should().HaveCount(3);
        result.AverageDailyChange.Should().BeGreaterThan(0);
        result.AverageWeeklyChange.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithDownwardTrend_ShouldReturnDownTrend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia a la baja (75kg → 72kg en 15 días)
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, new DateOnly(2026, 1, 1), 75.0),
            CreateWeightLog(userId, new DateOnly(2026, 1, 8), 73.5),
            CreateWeightLog(userId, new DateOnly(2026, 1, 15), 72.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.OverallTrend.Should().Be(WeightTrend.Down);
        result.DataPoints.Should().HaveCount(3);
        result.AverageDailyChange.Should().BeLessThan(0);
        result.AverageWeeklyChange.Should().BeLessThan(0);
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithNeutralTrend_ShouldReturnNeutralTrend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        _context.Set<Users>().Add(user);

        // Crear registros estables (70kg con variación menor a 100g)
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, new DateOnly(2026, 1, 1), 70.0),
            CreateWeightLog(userId, new DateOnly(2026, 1, 10), 70.05),
            CreateWeightLog(userId, new DateOnly(2026, 1, 20), 70.08)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.OverallTrend.Should().Be(WeightTrend.Neutral);
        result.DataPoints.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithSingleLog_ShouldReturnNeutralWithNoAverages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        _context.Set<Users>().Add(user);

        var log = CreateWeightLog(userId, new DateOnly(2026, 1, 10), 70.0);
        _context.Set<WeightLogs>().Add(log);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.OverallTrend.Should().Be(WeightTrend.Neutral);
        result.DataPoints.Should().HaveCount(1);
        result.AverageDailyChange.Should().BeNull(); // No hay rango para calcular
        result.AverageWeeklyChange.Should().BeNull();
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithLogsOutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        _context.Set<Users>().Add(user);

        // Logs fuera del rango consultado
        var log = CreateWeightLog(userId, new DateOnly(2025, 12, 15), 70.0);
        _context.Set<WeightLogs>().Add(log);
        await _context.SaveChangesAsync();

        var range = new DateRange
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31)
        };

        // Act
        var result = await _service.GetTrendAnalysisAsync(userId, range);

        // Assert
        result.DataPoints.Should().BeEmpty();
        result.OverallTrend.Should().Be(WeightTrend.Neutral);
    }

    [Fact]
    public async Task GetTrendAnalysisAsync_WithNullRange_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _service.GetTrendAnalysisAsync(userId, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetProjectionAsync Tests

    [Fact]
    public async Task GetProjectionAsync_WithInsufficientData_ShouldReturnProjectionWithoutWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        user.GoalWeight = 70.0;
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProjectionAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.ProjectedWeight.Should().BeNull();
        result.GoalWeight.Should().Be(70.0m);
        result.IsOnTrack.Should().BeFalse();
    }

    [Fact]
    public async Task GetProjectionAsync_WithLinearTrend_ShouldCalculateProjection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        user.GoalWeight = 68.0;
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia lineal descendente (75kg → 70kg en 30 días)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, thirtyDaysAgo, 75.0),
            CreateWeightLog(userId, thirtyDaysAgo.AddDays(10), 73.3),
            CreateWeightLog(userId, thirtyDaysAgo.AddDays(20), 71.7),
            CreateWeightLog(userId, today, 70.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProjectionAsync(userId);

        // Assert
        result.ProjectedWeight.Should().BeGreaterThan(0);
        result.ProjectedWeight.Should().BeLessThan(70.0m); // Tendencia descendente
        result.GoalWeight.Should().Be(68.0m);
    }

    [Fact]
    public async Task GetProjectionAsync_WithGoalAndFavorableTrend_ShouldCalculateEstimatedDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        user.GoalWeight = 68.0;
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia fuerte hacia el objetivo
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, thirtyDaysAgo, 75.0),
            CreateWeightLog(userId, thirtyDaysAgo.AddDays(15), 71.5),
            CreateWeightLog(userId, today, 69.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProjectionAsync(userId);

        // Assert
        result.EstimatedGoalDate.Should().NotBeNull();
        result.IsOnTrack.Should().BeTrue();
    }

    [Fact]
    public async Task GetProjectionAsync_WithGoalAndUnfavorableTrend_ShouldNotCalculateEstimatedDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        user.GoalWeight = 65.0; // Objetivo muy bajo
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia ascendente (opuesta al objetivo)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, thirtyDaysAgo, 70.0),
            CreateWeightLog(userId, thirtyDaysAgo.AddDays(15), 72.0),
            CreateWeightLog(userId, today, 74.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProjectionAsync(userId);

        // Assert
        result.EstimatedGoalDate.Should().BeNull();
        result.IsOnTrack.Should().BeFalse();
    }

    [Fact]
    public async Task GetProjectionAsync_WithoutGoalWeight_ShouldReturnProjectionWithoutGoalInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId);
        user.GoalWeight = null; // Sin objetivo
        _context.Set<Users>().Add(user);

        // Crear registros con tendencia
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        
        var logs = new List<WeightLogs>
        {
            CreateWeightLog(userId, thirtyDaysAgo, 75.0),
            CreateWeightLog(userId, today, 70.0)
        };
        _context.Set<WeightLogs>().AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProjectionAsync(userId);

        // Assert
        result.GoalWeight.Should().BeNull();
        result.EstimatedGoalDate.Should().BeNull();
        result.IsOnTrack.Should().BeFalse();
    }

    [Fact]
    public async Task GetProjectionAsync_WhenUserNotFound_ShouldThrowException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var act = async () => await _service.GetProjectionAsync(nonExistentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region Helper Methods

    private Users CreateUserEntity(Guid id)
    {
        return new Users
        {
            Id = id.ToString(),
            GoogleId = $"google_{id}",
            Name = "Test User",
            Email = $"user{id}@example.com",
            Role = (int)UserRole.User,
            MemberSince = DateTime.UtcNow.ToString("O"),
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            Language = "es",
            Status = (int)UserStatus.Active,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private WeightLogs CreateWeightLog(Guid userId, DateOnly date, double weight)
    {
        return new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Date = date.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Weight = weight,
            DisplayUnit = (int)WeightUnit.Kg,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    #endregion

    // Test DbContext for in-memory testing
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Users entity
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.GoogleId).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Email).IsRequired();
            });

            // Configure WeightLogs entity
            modelBuilder.Entity<WeightLogs>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Time).IsRequired();
                entity.Property(e => e.Weight).IsRequired();
            });
        }
    }
}
