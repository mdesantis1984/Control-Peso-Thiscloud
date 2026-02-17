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

public sealed class UserServiceTests : IDisposable
{
    private readonly DbContext _context;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new TestDbContext(options);
        _loggerMock = new Mock<ILogger<UserService>>();
        _service = new UserService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, "google_123", "Test User", "test@example.com");
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByGoogleIdAsync Tests

    [Fact]
    public async Task GetByGoogleIdAsync_WhenUserExists_ShouldReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleId = "google_12345";
        var user = CreateUserEntity(userId, googleId, "Test User", "test@example.com");
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByGoogleIdAsync(googleId);

        // Assert
        result.Should().NotBeNull();
        result!.GoogleId.Should().Be(googleId);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentGoogleId = "google_nonexistent";

        // Act
        var result = await _service.GetByGoogleIdAsync(nonExistentGoogleId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WithNullGoogleId_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.GetByGoogleIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WithEmptyGoogleId_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.GetByGoogleIdAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region CreateOrUpdateFromGoogleAsync Tests

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WhenUserDoesNotExist_ShouldCreateNewUser()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo
        {
            GoogleId = "google_new123",
            Email = "newuser@gmail.com",
            Name = "New User",
            AvatarUrl = "https://lh3.googleusercontent.com/avatar.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        result.Should().NotBeNull();
        result.GoogleId.Should().Be(googleInfo.GoogleId);
        result.Email.Should().Be(googleInfo.Email);
        result.Name.Should().Be(googleInfo.Name);
        result.AvatarUrl.Should().Be(googleInfo.AvatarUrl);
        result.Role.Should().Be(UserRole.User); // Default role
        result.Status.Should().Be(UserStatus.Active); // Default status

        // Verify user was saved to database
        var savedUser = await _context.Set<Users>().FirstOrDefaultAsync(u => u.GoogleId == googleInfo.GoogleId);
        savedUser.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WhenUserExists_ShouldUpdateExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleId = "google_existing";
        var existingUser = CreateUserEntity(userId, googleId, "Old Name", "old@example.com");
        existingUser.AvatarUrl = "https://old-avatar.com/image.jpg";
        _context.Set<Users>().Add(existingUser);
        await _context.SaveChangesAsync();

        var googleInfo = new GoogleUserInfo
        {
            GoogleId = googleId,
            Email = "updated@gmail.com",
            Name = "Updated Name",
            AvatarUrl = "https://new-avatar.com/image.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId); // Same user ID
        result.GoogleId.Should().Be(googleId);
        result.Email.Should().Be(googleInfo.Email); // Updated
        result.Name.Should().Be(googleInfo.Name); // Updated
        result.AvatarUrl.Should().Be(googleInfo.AvatarUrl); // Updated

        // Verify user count (no new user created)
        var userCount = await _context.Set<Users>().CountAsync();
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WhenUserExistsWithNoAvatar_ShouldUpdateWithAvatar()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleId = "google_noavatar";
        var existingUser = CreateUserEntity(userId, googleId, "User", "user@example.com");
        existingUser.AvatarUrl = null;
        _context.Set<Users>().Add(existingUser);
        await _context.SaveChangesAsync();

        var googleInfo = new GoogleUserInfo
        {
            GoogleId = googleId,
            Email = "user@example.com",
            Name = "User",
            AvatarUrl = "https://new-avatar.com/avatar.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        result.AvatarUrl.Should().Be(googleInfo.AvatarUrl);
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNullGoogleInfo_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.CreateOrUpdateFromGoogleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNullGoogleId_ShouldThrowException()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo
        {
            GoogleId = null!,
            Email = "test@example.com",
            Name = "Test"
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNullEmail_ShouldThrowException()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo
        {
            GoogleId = "google_123",
            Email = null!,
            Name = "Test"
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromGoogleAsync_WithNullName_ShouldThrowException()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo
        {
            GoogleId = "google_123",
            Email = "test@example.com",
            Name = null!
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromGoogleAsync(googleInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ShouldUpdateUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUserEntity(userId, "google_123", "Old Name", "test@example.com");
        user.Height = 170.0;
        user.Language = "es";
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserProfileDto
        {
            Name = "Updated Name",
            Height = 175.0m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = new DateOnly(1990, 5, 15),
            Language = "en",
            GoalWeight = 70.0m
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Height.Should().Be(175.0m);
        result.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        result.Language.Should().Be("en");
        result.GoalWeight.Should().Be(70.0m);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateUserProfileDto
        {
            Name = "Test",
            Height = 170.0m,
            UnitSystem = UnitSystem.Metric,
            Language = "es"
        };

        // Act
        var act = async () => await _service.UpdateProfileAsync(nonExistentId, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentId} not found.");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNullDto_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _service.UpdateProfileAsync(userId, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Alice", "alice@example.com");
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Bob", "bob@example.com");
        var user3 = CreateUserEntity(Guid.NewGuid(), "google_3", "Charlie", "charlie@example.com");
        
        _context.Set<Users>().AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ShouldReturnMatchingUsers()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Alice Johnson", "alice@example.com");
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Bob Smith", "bob@example.com");
        var user3 = CreateUserEntity(Guid.NewGuid(), "google_3", "Alice Brown", "charlie@example.com");
        
        _context.Set<Users>().AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10, SearchTerm = "Alice" };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(u => u.Name.Should().Contain("Alice"));
    }

    [Fact]
    public async Task GetAllAsync_WithRoleFilter_ShouldReturnUsersWithRole()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Admin User", "admin@example.com");
        user1.Role = (int)UserRole.Administrator;
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Normal User", "user@example.com");
        user2.Role = (int)UserRole.User;
        
        _context.Set<Users>().AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10, Role = UserRole.Administrator };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ShouldReturnUsersWithStatus()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Active User", "active@example.com");
        user1.Status = (int)UserStatus.Active;
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Inactive User", "inactive@example.com");
        user2.Status = (int)UserStatus.Inactive;
        
        _context.Set<Users>().AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10, Status = UserStatus.Active };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var user = CreateUserEntity(Guid.NewGuid(), $"google_{i}", $"User {i}", $"user{i}@example.com");
            _context.Set<Users>().Add(user);
        }
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 2, PageSize = 5 };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithDescendingSortFalse_ShouldReturnAscendingOrder()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Charlie", "charlie@example.com");
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Alice", "alice@example.com");
        var user3 = CreateUserEntity(Guid.NewGuid(), "google_3", "Bob", "bob@example.com");
        
        _context.Set<Users>().AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10, Descending = false };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alice");
        result.Items[1].Name.Should().Be("Bob");
        result.Items[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetAllAsync_WithDescendingSortTrue_ShouldReturnDescendingOrder()
    {
        // Arrange
        var user1 = CreateUserEntity(Guid.NewGuid(), "google_1", "Charlie", "charlie@example.com");
        var user2 = CreateUserEntity(Guid.NewGuid(), "google_2", "Alice", "alice@example.com");
        var user3 = CreateUserEntity(Guid.NewGuid(), "google_3", "Bob", "bob@example.com");
        
        _context.Set<Users>().AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        var filter = new UserFilter { Page = 1, PageSize = 10, Descending = true };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Charlie");
        result.Items[1].Name.Should().Be("Bob");
        result.Items[2].Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetAllAsync_WithNullFilter_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.GetAllAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helper Methods

    private Users CreateUserEntity(Guid id, string googleId, string name, string email)
    {
        return new Users
        {
            Id = id.ToString(),
            GoogleId = googleId,
            Name = name,
            Email = email,
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
