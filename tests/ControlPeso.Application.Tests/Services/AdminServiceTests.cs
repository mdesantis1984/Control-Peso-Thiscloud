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
    private readonly DbContext _context;
    private readonly Mock<ILogger<AdminService>> _loggerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new TestDbContext(options);
        _loggerMock = new Mock<ILogger<AdminService>>();
        _userServiceMock = new Mock<IUserService>();
        _service = new AdminService(_context, _loggerMock.Object, _userServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

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
        
        _context.Set<Users>().AddRange(user1, user2, user3, user4);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var log1 = CreateWeightLog(Guid.Parse(user1.Id), today);
        var log2 = CreateWeightLog(Guid.Parse(user1.Id), today.AddDays(-5));
        var log3 = CreateWeightLog(Guid.Parse(user2.Id), today.AddDays(-20));
        
        _context.Set<WeightLogs>().AddRange(log1, log2, log3);
        await _context.SaveChangesAsync();

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
        _context.Set<Users>().Add(user);

        // Logs de hace más de 30 días
        var log = CreateWeightLog(userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)));
        _context.Set<WeightLogs>().Add(log);
        await _context.SaveChangesAsync();

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
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateUserRoleAsync(userId, UserRole.Administrator);

        // Assert
        var updatedUser = await _context.Set<Users>().FirstAsync(u => u.Id == userId.ToString());
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
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateUserStatusAsync(userId, UserStatus.Inactive);

        // Assert
        var updatedUser = await _context.Set<Users>().FirstAsync(u => u.Id == userId.ToString());
        updatedUser.Status.Should().Be((int)UserStatus.Inactive);

        // Verificar audit log creado
        var auditLog = await _context.Set<AuditLog>().FirstOrDefaultAsync(a => a.EntityId == userId.ToString());
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

    #region Helper Methods

    private Users CreateUserEntity(Guid id, UserStatus status)
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
            Status = (int)status,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };
    }

    private WeightLogs CreateWeightLog(Guid userId, DateOnly date)
    {
        return new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.ToString(),
            Date = date.ToString("yyyy-MM-dd"),
            Time = "12:00",
            Weight = 70.0,
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
