using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Application.Tests.Services;

public sealed class AdminServiceTests : IDisposable
{
    private readonly DbContextOptions<DbContext> _contextOptions;
    private readonly Mock<IDbContextFactory<DbContext>> _contextFactoryMock;
    private readonly Mock<ILogger<AdminService>> _loggerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly AdminService _service;
    private readonly TestDbContext _context; // TEMP: For legacy test assertions - will be removed

    public AdminServiceTests()
    {
        _contextOptions = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        // TEMP: Single context for legacy test assertions
        _context = new TestDbContext(_contextOptions);

        // Mock IDbContextFactory to return NEW context instance per call (avoid tracking conflicts)
        _contextFactoryMock = new Mock<IDbContextFactory<DbContext>>();
        _contextFactoryMock
            .Setup(f => f.CreateDbContext())
            .Returns(() => new TestDbContext(_contextOptions));
        _contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TestDbContext(_contextOptions));

        _loggerMock = new Mock<ILogger<AdminService>>();
        _userServiceMock = new Mock<IUserService>();
        _service = new AdminService(_contextFactoryMock.Object, _loggerMock.Object, _userServiceMock.Object);
    }

    public void Dispose()
    {
        // Clean up in-memory database
        _context.Dispose();
        using var context = new TestDbContext(_contextOptions);
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    /// <summary>
    /// Helper to seed data into the in-memory database.
    /// Call this at the start of tests that need existing data.
    /// </summary>
    private async Task SeedDataAsync(params object[] entities)
    {
        using var context = new TestDbContext(_contextOptions);
        foreach (var entity in entities)
        {
            context.Add(entity);
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper to get a context for test assertions.
    /// Use for verification queries ONLY (not for setup).
    /// </summary>
    private TestDbContext GetContext() => new TestDbContext(_contextOptions);

    #region GetDashboardAsync Tests

    [Fact]
    public async Task GetDashboardAsync_WithNoData_ShouldReturnZeroCounts()
    {
        // Act
        var result = await _service.GetDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalUsers.Should().Be(0);
        result.ActiveUsers.Should().Be(0);
        result.PendingUsers.Should().Be(0);
        result.InactiveUsers.Should().Be(0);
        result.TotalWeightLogs.Should().Be(0);
        result.WeightLogsLastWeek.Should().Be(0);
        result.WeightLogsLastMonth.Should().Be(0);
        result.LatestUserRegistration.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_WithUsersAndLogs_ShouldReturnCorrectCounts()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), UserStatus.Active);
        var user2 = CreateUserEntity(Guid.NewGuid(), UserStatus.Active);
        var user3 = CreateUserEntity(Guid.NewGuid(), UserStatus.Inactive);
        var user4 = CreateUserEntity(Guid.NewGuid(), UserStatus.Pending);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var log1 = CreateWeightLog(user1.Id, today);
        var log2 = CreateWeightLog(user1.Id, today.AddDays(-5));
        var log3 = CreateWeightLog(user2.Id, today.AddDays(-20));

        await SeedDataAsync(user1, user2, user3, user4, log1, log2, log3);

        // Act
        var result = await _service.GetDashboardAsync();

        // Assert
        result.TotalUsers.Should().Be(4);
        result.ActiveUsers.Should().Be(2);
        result.InactiveUsers.Should().Be(1);
        result.PendingUsers.Should().Be(1);
        result.TotalWeightLogs.Should().Be(3);
        result.WeightLogsLastWeek.Should().Be(2); // log1 y log2
        result.WeightLogsLastMonth.Should().Be(3); // todos
        result.LatestUserRegistration.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardAsync_WithOnlyOldLogs_ShouldReturnZeroRecentCounts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, UserStatus.Active);

        // Logs de hace más de 30 días
        var log = CreateWeightLog(userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)));

        await SeedDataAsync(user, log);

        // Act
        var result = await _service.GetDashboardAsync();

        // Assert
        result.TotalWeightLogs.Should().Be(1);
        result.WeightLogsLastWeek.Should().Be(0);
        result.WeightLogsLastMonth.Should().Be(0);
    }

    #endregion

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_ShouldDelegateToUserService()
    {
        // Arrange
        var filter = new UserFilter { Page = 1, PageSize = 10 };
        var expectedResult = new PagedResult<UserDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _userServiceMock
            .Setup(x => x.GetAllAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetUsersAsync(filter);

        // Assert
        result.Should().BeSameAs(expectedResult);
        _userServiceMock.Verify(x => x.GetAllAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUsersAsync_WithNullFilter_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.GetUsersAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateUserRoleAsync Tests

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidData_ShouldUpdateRoleAndCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, UserStatus.Active);
        user.Role = (int)UserRole.User;
        await SeedDataAsync(user);

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.Administrator);

        // Assert
        using var context = GetContext();
        var updatedUser = await context.Set<Users>().FirstAsync(u => u.Id == userId);
        updatedUser.Role.Should().Be((int)UserRole.Administrator);

        // Verificar audit log creado
        var auditLog = await _context.Set<AuditLog>().FirstOrDefaultAsync(a => a.EntityId == userId.ToString());
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("UserRoleChanged");
        auditLog.EntityType.Should().Be("User");

        // JSON serializa enums como números (0 = User, 1 = Administrator)
        auditLog.OldValue.Should().Contain("\"Role\":0"); // User
        auditLog.NewValue.Should().Contain("\"Role\":1"); // Administrator
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithSameRole_ShouldNotUpdateOrCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, UserStatus.Active);
        user.Role = (int)UserRole.User;
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.User); // Mismo rol

        // Assert
        var auditLogCount = await _context.Set<AuditLog>().CountAsync();
        auditLogCount.Should().Be(0); // No debería crear audit log
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserNotFound_ShouldThrowException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var act = async () => await _service.UpdateUserRoleAsync(nonExistentUserId, UserRole.Administrator);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region UpdateUserStatusAsync Tests

    [Fact]
    public async Task UpdateUserStatusAsync_WithValidData_ShouldUpdateStatusAndCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, UserStatus.Active);
        await SeedDataAsync(user);

        // Act
        await _service.UpdateUserStatusAsync(userId, UserStatus.Inactive);

        // Assert - Create FRESH context to see persisted changes
        using var verificationContext = GetContext();
        var updatedUser = await verificationContext.Set<Users>().FirstAsync(u => u.Id == userId);
        updatedUser.Status.Should().Be((int)UserStatus.Inactive);

        // Verificar audit log creado
        var auditLog = await verificationContext.Set<AuditLog>().FirstOrDefaultAsync(a => a.EntityId == userId.ToString());
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("UserStatusChanged");
        auditLog.EntityType.Should().Be("User");

        // JSON serializa enums como números (0 = Active, 1 = Inactive)
        auditLog.OldValue.Should().Contain("\"Status\":0"); // Active
        auditLog.NewValue.Should().Contain("\"Status\":1"); // Inactive
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WithSameStatus_ShouldNotUpdateOrCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, UserStatus.Active);
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateUserStatusAsync(userId, UserStatus.Active); // Mismo estado

        // Assert
        var auditLogCount = await _context.Set<AuditLog>().CountAsync();
        auditLogCount.Should().Be(0); // No debería crear audit log
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenUserNotFound_ShouldThrowException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var act = async () => await _service.UpdateUserStatusAsync(nonExistentUserId, UserStatus.Inactive);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region Database Error Scenarios

    [Fact]
    public async Task GetDashboardAsync_WhenDatabaseError_ShouldThrowException()
    {
        // Arrange
        var factoryMock = new Mock<IDbContextFactory<DbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new AdminService(factoryMock.Object, _loggerMock.Object, _userServiceMock.Object);

        // Act
        var act = async () => await service.GetDashboardAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _service.UpdateUserRoleAsync(nonExistentId, UserRole.Administrator);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentId} not found.");
    }

    [Fact]
    public async Task UpdateUserStatusAsync_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _service.UpdateUserStatusAsync(nonExistentId, UserStatus.Inactive);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentId} not found.");
    }

    #endregion

    #region Helper Methods

    private static Users CreateUserEntity(Guid id, UserStatus status)
    {
        return new Users
        {
            Id = id,
            GoogleId = $"google_{id}",
            Name = "Test User",
            Email = $"user{id}@example.com",
            Role = (int)UserRole.User,
            MemberSince = DateTime.UtcNow,
            Height = 170.0m,
            UnitSystem = (int)UnitSystem.Metric,
            Language = "es",
            Status = (int)status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static WeightLogs CreateWeightLog(Guid userId, DateOnly date)
    {
        return new WeightLogs
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = date,
            Time = new TimeOnly(12, 00),
            Weight = 70.0m,
            DisplayUnit = (int)WeightUnit.Kg,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = DateTime.UtcNow
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
            });

            // Configure WeightLogs entity
            modelBuilder.Entity<WeightLogs>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
            });
        }
    }
}
