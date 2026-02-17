using ControlPeso.Application.DTOs;
using ControlPeso.Application.Mapping;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Mapping;

public sealed class UserMapperTests
{
    [Fact]
    public void ToDto_WithValidEntity_ShouldConvertCorrectly()
    {
        // Arrange
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = "https://example.com/avatar.jpg",
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 175.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = "1990-05-15",
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = 70.0,
            StartingWeight = 80.0,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = "2026-02-17T14:30:00.0000000Z"
        };

        // Act
        var dto = UserMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(Guid.Parse(entity.Id));
        dto.GoogleId.Should().Be("google123");
        dto.Name.Should().Be("Test User");
        dto.Email.Should().Be("test@example.com");
        dto.Role.Should().Be(UserRole.User);
        dto.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        dto.MemberSince.Should().Be(DateTime.Parse("2026-01-01T00:00:00.0000000Z"));
        dto.Height.Should().Be(175.0m);
        dto.UnitSystem.Should().Be(UnitSystem.Metric);
        dto.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        dto.Language.Should().Be("es");
        dto.Status.Should().Be(UserStatus.Active);
        dto.GoalWeight.Should().Be(70.0m);
        dto.StartingWeight.Should().Be(80.0m);
        dto.CreatedAt.Should().Be(DateTime.Parse("2026-01-01T00:00:00.0000000Z"));
        dto.UpdatedAt.Should().Be(DateTime.Parse("2026-02-17T14:30:00.0000000Z"));
    }

    [Fact]
    public void ToDto_WithNullableFieldsNull_ShouldHandleCorrectly()
    {
        // Arrange
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = null,
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = "2026-01-01T00:00:00.0000000Z"
        };

        // Act
        var dto = UserMapper.ToDto(entity);

        // Assert
        dto.AvatarUrl.Should().BeNull();
        dto.DateOfBirth.Should().BeNull();
        dto.GoalWeight.Should().BeNull();
        dto.StartingWeight.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        Users? entity = null;

        // Act
        var act = () => UserMapper.ToDto(entity!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToEntity_WithValidGoogleInfo_ShouldCreateEntityWithDefaults()
    {
        // Arrange
        var info = new GoogleUserInfo
        {
            GoogleId = "google123",
            Name = "New User",
            Email = "newuser@example.com",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var entity = UserMapper.ToEntity(info);

        // Assert
        Guid.TryParse(entity.Id, out var id).Should().BeTrue();
        id.Should().NotBeEmpty();
        entity.GoogleId.Should().Be("google123");
        entity.Name.Should().Be("New User");
        entity.Email.Should().Be("newuser@example.com");
        entity.Role.Should().Be((int)UserRole.User);
        entity.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        entity.Height.Should().Be(170.0);
        entity.UnitSystem.Should().Be((int)UnitSystem.Metric);
        entity.DateOfBirth.Should().BeNull();
        entity.Language.Should().Be("es");
        entity.Status.Should().Be((int)UserStatus.Active);
        entity.GoalWeight.Should().BeNull();
        entity.StartingWeight.Should().BeNull();
        DateTime.TryParse(entity.MemberSince, out var memberSince).Should().BeTrue();
        memberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
        DateTime.TryParse(entity.CreatedAt, out var createdAt).Should().BeTrue();
        createdAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
        DateTime.TryParse(entity.UpdatedAt, out var updatedAt).Should().BeTrue();
        updatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
    }

    [Fact]
    public void ToEntity_WithNullGoogleInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        GoogleUserInfo? info = null;

        // Act
        var act = () => UserMapper.ToEntity(info!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateEntity_WithValidData_ShouldUpdateOnlyEditableFields()
    {
        // Arrange
        var originalId = Guid.NewGuid().ToString();
        var originalGoogleId = "google123";
        var originalEmail = "original@example.com";
        var originalRole = (int)UserRole.Administrator;
        var originalMemberSince = "2026-01-01T00:00:00.0000000Z";
        var originalStatus = (int)UserStatus.Active;
        var originalCreatedAt = "2026-01-01T00:00:00.0000000Z";

        var entity = new Users
        {
            Id = originalId,
            GoogleId = originalGoogleId,
            Name = "Old Name",
            Email = originalEmail,
            Role = originalRole,
            AvatarUrl = "https://example.com/old-avatar.jpg",
            MemberSince = originalMemberSince,
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "en",
            Status = originalStatus,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = originalCreatedAt,
            UpdatedAt = "2026-02-01T00:00:00.0000000Z"
        };

        var updateDto = new UpdateUserProfileDto
        {
            Name = "Updated Name",
            Height = 180.0m,
            UnitSystem = UnitSystem.Imperial,
            DateOfBirth = new DateOnly(1990, 5, 15),
            Language = "es",
            GoalWeight = 75.0m
        };

        // Act
        UserMapper.UpdateEntity(entity, updateDto);

        // Assert
        // Should update these fields
        entity.Name.Should().Be("Updated Name");
        entity.Height.Should().Be(180.0);
        entity.UnitSystem.Should().Be((int)UnitSystem.Imperial);
        entity.DateOfBirth.Should().Be("1990-05-15");
        entity.Language.Should().Be("es");
        entity.GoalWeight.Should().Be(75.0);
        entity.UpdatedAt.Should().NotBe("2026-02-01T00:00:00.0000000Z");
        DateTime.TryParse(entity.UpdatedAt, out _).Should().BeTrue();

        // Should NOT modify these fields
        entity.Id.Should().Be(originalId);
        entity.GoogleId.Should().Be(originalGoogleId);
        entity.Email.Should().Be(originalEmail);
        entity.Role.Should().Be(originalRole);
        entity.MemberSince.Should().Be(originalMemberSince);
        entity.Status.Should().Be(originalStatus);
        entity.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void UpdateEntity_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        Users? entity = null;
        var dto = new UpdateUserProfileDto
        {
            Name = "Test",
            Height = 175.0m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        // Act
        var act = () => UserMapper.UpdateEntity(entity!, dto);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateEntity_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = null,
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = "2026-01-01T00:00:00.0000000Z"
        };
        UpdateUserProfileDto? dto = null;

        // Act
        var act = () => UserMapper.UpdateEntity(entity, dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateFromGoogle_WhenNameChanged_ShouldUpdateNameAndTimestamp()
    {
        // Arrange
        var originalUpdatedAt = "2026-02-01T00:00:00.0000000Z";
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Old Name",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = "https://example.com/avatar.jpg",
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = originalUpdatedAt
        };

        var info = new GoogleUserInfo
        {
            GoogleId = "google123",
            Name = "New Name",
            Email = "test@example.com",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        UserMapper.UpdateFromGoogle(entity, info);

        // Assert
        entity.Name.Should().Be("New Name");
        entity.UpdatedAt.Should().NotBe(originalUpdatedAt);
        DateTime.TryParse(entity.UpdatedAt, out _).Should().BeTrue();
    }

    [Fact]
    public void UpdateFromGoogle_WhenAvatarChanged_ShouldUpdateAvatarAndTimestamp()
    {
        // Arrange
        var originalUpdatedAt = "2026-02-01T00:00:00.0000000Z";
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = "https://example.com/old-avatar.jpg",
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = originalUpdatedAt
        };

        var info = new GoogleUserInfo
        {
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = "https://example.com/new-avatar.jpg"
        };

        // Act
        UserMapper.UpdateFromGoogle(entity, info);

        // Assert
        entity.AvatarUrl.Should().Be("https://example.com/new-avatar.jpg");
        entity.UpdatedAt.Should().NotBe(originalUpdatedAt);
        DateTime.TryParse(entity.UpdatedAt, out _).Should().BeTrue();
    }

    [Fact]
    public void UpdateFromGoogle_WhenNothingChanged_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var originalUpdatedAt = "2026-02-01T00:00:00.0000000Z";
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = "https://example.com/avatar.jpg",
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = originalUpdatedAt
        };

        var info = new GoogleUserInfo
        {
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        UserMapper.UpdateFromGoogle(entity, info);

        // Assert
        entity.Name.Should().Be("Test User");
        entity.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        entity.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdateFromGoogle_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        Users? entity = null;
        var info = new GoogleUserInfo
        {
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var act = () => UserMapper.UpdateFromGoogle(entity!, info);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateFromGoogle_WithNullInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new Users
        {
            Id = Guid.NewGuid().ToString(),
            GoogleId = "google123",
            Name = "Test User",
            Email = "test@example.com",
            Role = (int)UserRole.User,
            AvatarUrl = null,
            MemberSince = "2026-01-01T00:00:00.0000000Z",
            Height = 170.0,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = "2026-01-01T00:00:00.0000000Z",
            UpdatedAt = "2026-01-01T00:00:00.0000000Z"
        };
        GoogleUserInfo? info = null;

        // Act
        var act = () => UserMapper.UpdateFromGoogle(entity, info!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
