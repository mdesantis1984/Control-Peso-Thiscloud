using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlPeso.Application.Tests.Services;

public sealed class UserServiceTests : IDisposable
{
    private readonly DbContextOptions<DbContext> _contextOptions;
    private readonly Mock<IDbContextFactory<DbContext>> _contextFactoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly UserService _service;
    private readonly TestDbContext _context; // TEMP: For legacy test assertions - will be removed

    public UserServiceTests()
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

        _loggerMock = new Mock<ILogger<UserService>>();

        // Create real MemoryCache for testing (simpler than mocking)
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _service = new UserService(_contextFactoryMock.Object, _memoryCache, _loggerMock.Object);
    }

    public void Dispose()
    {
        // Clean up in-memory database
        _context.Dispose();
        using var context = new TestDbContext(_contextOptions);
        context.Database.EnsureDeleted();
        context.Dispose();
        _memoryCache.Dispose();
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
        user.Height = 170.0m;
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

    private static Users CreateUserEntity(Guid id, string googleId, string name, string email)
    {
        return new Users
        {
            Id = id,
            GoogleId = googleId,
            Name = name,
            Email = email,
            Role = (int)UserRole.User,
            MemberSince = DateTime.UtcNow,
            Height = 170.0m,
            UnitSystem = (int)UnitSystem.Metric,
            Language = "es",
            Status = (int)UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ShouldReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var user = CreateUserEntity(userId, "google_123", "Test User", email);
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@example.com";

        // Act
        var result = await _service.GetByEmailAsync(nonExistentEmail);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithNullEmail_ShouldThrowException()
    {
        // Arrange
        string? email = null;

        // Act
        var act = async () => await _service.GetByEmailAsync(email!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByEmailAsync_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange
        var email = "";

        // Act
        var act = async () => await _service.GetByEmailAsync(email);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByEmailAsync_WithWhitespaceEmail_ShouldThrowException()
    {
        // Arrange
        var email = "   ";

        // Act
        var act = async () => await _service.GetByEmailAsync(email);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetByLinkedInIdAsync Tests

    [Fact]
    public async Task GetByLinkedInIdAsync_WhenUserExists_ShouldReturnDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var linkedInId = "linkedin_12345";
        var user = CreateUserEntity(userId, $"google_{Guid.NewGuid()}", "LinkedIn User", $"linkedin{Guid.NewGuid()}@example.com");
        user.LinkedInId = linkedInId;
        _context.Set<Users>().Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByLinkedInIdAsync(linkedInId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("LinkedIn User");
        var savedUser = await _context.Set<Users>().FirstAsync(u => u.Id == result.Id);
        savedUser.LinkedInId.Should().Be(linkedInId);
    }

    [Fact]
    public async Task GetByLinkedInIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentLinkedInId = "linkedin_nonexistent";

        // Act
        var result = await _service.GetByLinkedInIdAsync(nonExistentLinkedInId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByLinkedInIdAsync_WithNullLinkedInId_ShouldThrowException()
    {
        // Arrange
        string? linkedInId = null;

        // Act
        var act = async () => await _service.GetByLinkedInIdAsync(linkedInId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByLinkedInIdAsync_WithEmptyLinkedInId_ShouldThrowException()
    {
        // Arrange
        var linkedInId = "";

        // Act
        var act = async () => await _service.GetByLinkedInIdAsync(linkedInId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByLinkedInIdAsync_WithWhitespaceLinkedInId_ShouldThrowException()
    {
        // Arrange
        var linkedInId = "   ";

        // Act
        var act = async () => await _service.GetByLinkedInIdAsync(linkedInId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region CreateOrUpdateFromOAuthAsync Tests

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithGoogleProvider_WhenUserDoesNotExist_ShouldCreateNewUser()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "google_new_123",
            Name = "New Google User",
            Email = "newgoogle@example.com",
            AvatarUrl = "https://google.com/avatar.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        result.Should().NotBeNull();
        result.GoogleId.Should().Be("google_new_123");
        result.Name.Should().Be("New Google User");
        result.Email.Should().Be("newgoogle@example.com");
        result.AvatarUrl.Should().Be("https://google.com/avatar.jpg");
        result.Role.Should().Be(UserRole.User);
        result.Status.Should().Be(UserStatus.Active);
        // Verify LinkedIn not set
        var savedUser = await _context.Set<Users>().FirstAsync(u => u.Id == result.Id);
        savedUser.LinkedInId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithLinkedInProvider_WhenUserDoesNotExist_ShouldCreateNewUser()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "LinkedIn",
            ExternalId = "linkedin_new_123",
            Name = "New LinkedIn User",
            Email = $"newlinkedin{Guid.NewGuid()}@example.com",
            AvatarUrl = "https://linkedin.com/avatar.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        result.Should().NotBeNull();
        result.GoogleId.Should().BeNullOrEmpty(); // GoogleId is required string, so it's "" for LinkedIn users
        result.Name.Should().Be("New LinkedIn User");
        result.Email.Should().Contain("newlinkedin");
        result.AvatarUrl.Should().Be("https://linkedin.com/avatar.jpg");
        result.Role.Should().Be(UserRole.User);
        result.Status.Should().Be(UserStatus.Active);
        // Verify LinkedInId in entity
        var savedUser = await _context.Set<Users>().FirstAsync(u => u.Id == result.Id);
        savedUser.LinkedInId.Should().Be("linkedin_new_123");
        savedUser.GoogleId.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithGoogleProvider_WhenUserExists_ShouldUpdateExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleId = "google_existing_123";
        var existingUser = CreateUserEntity(userId, googleId, "Old Name", "old@example.com");
        existingUser.AvatarUrl = "https://old-avatar.com/pic.jpg";
        _context.Set<Users>().Add(existingUser);
        await _context.SaveChangesAsync();

        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = googleId,
            Name = "Updated Name",
            Email = "updated@example.com",
            AvatarUrl = "https://new-avatar.com/pic.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.GoogleId.Should().Be(googleId);
        result.Name.Should().Be("Updated Name");
        result.Email.Should().Be("updated@example.com");
        result.AvatarUrl.Should().Be("https://new-avatar.com/pic.jpg");
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithLinkedInProvider_WhenUserExists_ShouldUpdateExistingUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var linkedInId = "linkedin_existing_123";
        var existingUser = CreateUserEntity(userId, $"google_{Guid.NewGuid()}", "Old LinkedIn Name", $"oldlinkedin{Guid.NewGuid()}@example.com");
        existingUser.LinkedInId = linkedInId;
        existingUser.AvatarUrl = "https://old-linkedin-avatar.com/pic.jpg";
        _context.Set<Users>().Add(existingUser);
        await _context.SaveChangesAsync();

        var oauthInfo = new OAuthUserInfo
        {
            Provider = "LinkedIn",
            ExternalId = linkedInId,
            Name = "Updated LinkedIn Name",
            Email = $"updatedlinkedin{Guid.NewGuid()}@example.com",
            AvatarUrl = "https://new-linkedin-avatar.com/pic.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Name.Should().Be("Updated LinkedIn Name");
        result.Email.Should().Contain("updatedlinkedin");
        result.AvatarUrl.Should().Be("https://new-linkedin-avatar.com/pic.jpg");
        // Verify LinkedInId in entity
        var savedUser = await _context.Set<Users>().FirstAsync(u => u.Id == userId);
        savedUser.LinkedInId.Should().Be(linkedInId);
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithCustomAvatar_ShouldPreserveCustomAvatar()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var googleId = "google_custom_avatar_123";
        var customAvatarUrl = "/uploads/avatars/custom-123.jpg";
        var existingUser = CreateUserEntity(userId, googleId, "User With Custom Avatar", "custom@example.com");
        existingUser.AvatarUrl = customAvatarUrl;
        _context.Set<Users>().Add(existingUser);
        await _context.SaveChangesAsync();

        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = googleId,
            Name = "User With Custom Avatar",
            Email = "custom@example.com",
            AvatarUrl = "https://google.com/new-avatar.jpg"
        };

        // Act
        var result = await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be(customAvatarUrl); // Custom avatar preserved
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithUnsupportedProvider_ShouldThrowNotSupportedException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Facebook", // Unsupported provider
            ExternalId = "facebook_123",
            Name = "Facebook User",
            Email = "facebook@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Facebook*not supported*");
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithNullOAuthInfo_ShouldThrowException()
    {
        // Arrange
        OAuthUserInfo? oauthInfo = null;

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithNullProvider_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = null!,
            ExternalId = "external_123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithEmptyProvider_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "",
            ExternalId = "external_123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithWhitespaceProvider_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "   ",
            ExternalId = "external_123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithNullExternalId_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = null!,
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithEmptyExternalId_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithWhitespaceExternalId_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "   ",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithNullEmail_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = "Test User",
            Email = null!,
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithEmptyEmail_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = "Test User",
            Email = "",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithWhitespaceEmail_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = "Test User",
            Email = "   ",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithNullName_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = null!,
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = "",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateOrUpdateFromOAuthAsync_WithWhitespaceName_ShouldThrowException()
    {
        // Arrange
        var oauthInfo = new OAuthUserInfo
        {
            Provider = "Google",
            ExternalId = "external_123",
            Name = "   ",
            Email = "test@example.com",
            AvatarUrl = null
        };

        // Act
        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(oauthInfo);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Database Error Scenarios

    [Fact]
    public async Task GetByIdAsync_WhenDatabaseError_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Create a factory that throws on CreateDbContextAsync
        var factoryMock = new Mock<IDbContextFactory<DbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserService(factoryMock.Object, _memoryCache, _loggerMock.Object);

        // Act
        var act = async () => await service.GetByIdAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetByGoogleIdAsync_WhenDatabaseError_ShouldThrowException()
    {
        // Arrange
        var googleId = "google_123";

        // Create a factory that throws on CreateDbContextAsync
        var factoryMock = new Mock<IDbContextFactory<DbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserService(factoryMock.Object, _memoryCache, _loggerMock.Object);

        // Act
        var act = async () => await service.GetByGoogleIdAsync(googleId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetByLinkedInIdAsync_WhenDatabaseError_ShouldThrowException()
    {
        // Arrange
        var linkedInId = "linkedin_123";

        // Create a factory that throws on CreateDbContextAsync
        var factoryMock = new Mock<IDbContextFactory<DbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserService(factoryMock.Object, _memoryCache, _loggerMock.Object);

        // Act
        var act = async () => await service.GetByLinkedInIdAsync(linkedInId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenDatabaseError_ShouldThrowException()
    {
        // Arrange
        var email = "test@example.com";

        // Create a factory that throws on CreateDbContextAsync
        var factoryMock = new Mock<IDbContextFactory<DbContext>>();
        factoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = new UserService(factoryMock.Object, _memoryCache, _loggerMock.Object);

        // Act
        var act = async () => await service.GetByEmailAsync(email);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new UpdateUserProfileDto
        {
            Name = "Updated Name",
            Height = 175.0m,
            Language = "en",
            UnitSystem = UnitSystem.Metric
        };

        // Act
        var act = async () => await _service.UpdateProfileAsync(nonExistentId, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentId} not found.");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        UpdateUserProfileDto? dto = null;

        // Act
        var act = async () => await _service.UpdateProfileAsync(userId, dto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
                entity.Property(e => e.GoogleId).IsRequired(false); // Nullable para LinkedIn users
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
