using ControlPeso.Domain.Entities;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Entities;

public class UsersTests
{
    [Fact]
    public void Users_ShouldSetAndGetId()
    {
        // Arrange
        var user = new Users();
        const string id = "test-id-123";

        // Act
        user.Id = id;

        // Assert
        user.Id.Should().Be(id);
    }

    [Fact]
    public void Users_ShouldSetAndGetGoogleId()
    {
        // Arrange
        var user = new Users();
        const string googleId = "google-123";

        // Act
        user.GoogleId = googleId;

        // Assert
        user.GoogleId.Should().Be(googleId);
    }

    [Fact]
    public void Users_ShouldSetAndGetLinkedInId()
    {
        // Arrange
        var user = new Users();
        const string linkedInId = "linkedin-123";

        // Act
        user.LinkedInId = linkedInId;

        // Assert
        user.LinkedInId.Should().Be(linkedInId);
    }

    [Fact]
    public void Users_ShouldSetAndGetName()
    {
        // Arrange
        var user = new Users();
        const string name = "Test User";

        // Act
        user.Name = name;

        // Assert
        user.Name.Should().Be(name);
    }

    [Fact]
    public void Users_ShouldSetAndGetEmail()
    {
        // Arrange
        var user = new Users();
        const string email = "test@example.com";

        // Act
        user.Email = email;

        // Assert
        user.Email.Should().Be(email);
    }

    [Fact]
    public void Users_ShouldSetAndGetRole()
    {
        // Arrange
        var user = new Users();
        const int role = 1;

        // Act
        user.Role = role;

        // Assert
        user.Role.Should().Be(role);
    }

    [Fact]
    public void Users_ShouldSetAndGetAvatarUrl()
    {
        // Arrange
        var user = new Users();
        const string avatarUrl = "https://example.com/avatar.jpg";

        // Act
        user.AvatarUrl = avatarUrl;

        // Assert
        user.AvatarUrl.Should().Be(avatarUrl);
    }

    [Fact]
    public void Users_ShouldSetAndGetMemberSince()
    {
        // Arrange
        var user = new Users();
        const string memberSince = "2024-01-01T00:00:00Z";

        // Act
        user.MemberSince = memberSince;

        // Assert
        user.MemberSince.Should().Be(memberSince);
    }

    [Fact]
    public void Users_ShouldSetAndGetHeight()
    {
        // Arrange
        var user = new Users();
        const double height = 175.5;

        // Act
        user.Height = height;

        // Assert
        user.Height.Should().Be(height);
    }

    [Fact]
    public void Users_ShouldSetAndGetUnitSystem()
    {
        // Arrange
        var user = new Users();
        const int unitSystem = 0;

        // Act
        user.UnitSystem = unitSystem;

        // Assert
        user.UnitSystem.Should().Be(unitSystem);
    }

    [Fact]
    public void Users_ShouldSetAndGetDateOfBirth()
    {
        // Arrange
        var user = new Users();
        const string dateOfBirth = "1990-01-01";

        // Act
        user.DateOfBirth = dateOfBirth;

        // Assert
        user.DateOfBirth.Should().Be(dateOfBirth);
    }

    [Fact]
    public void Users_ShouldSetAndGetLanguage()
    {
        // Arrange
        var user = new Users();
        const string language = "es";

        // Act
        user.Language = language;

        // Assert
        user.Language.Should().Be(language);
    }

    [Fact]
    public void Users_ShouldSetAndGetStatus()
    {
        // Arrange
        var user = new Users();
        const int status = 0;

        // Act
        user.Status = status;

        // Assert
        user.Status.Should().Be(status);
    }

    [Fact]
    public void Users_ShouldSetAndGetGoalWeight()
    {
        // Arrange
        var user = new Users();
        const double goalWeight = 75.5;

        // Act
        user.GoalWeight = goalWeight;

        // Assert
        user.GoalWeight.Should().Be(goalWeight);
    }

    [Fact]
    public void Users_ShouldSetAndGetStartingWeight()
    {
        // Arrange
        var user = new Users();
        const double startingWeight = 85.5;

        // Act
        user.StartingWeight = startingWeight;

        // Assert
        user.StartingWeight.Should().Be(startingWeight);
    }

    [Fact]
    public void Users_ShouldSetAndGetCreatedAt()
    {
        // Arrange
        var user = new Users();
        const string createdAt = "2024-01-01T00:00:00Z";

        // Act
        user.CreatedAt = createdAt;

        // Assert
        user.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Users_ShouldSetAndGetUpdatedAt()
    {
        // Arrange
        var user = new Users();
        const string updatedAt = "2024-01-02T00:00:00Z";

        // Act
        user.UpdatedAt = updatedAt;

        // Assert
        user.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Users_ShouldInitializeNavigationProperties()
    {
        // Act
        var user = new Users();

        // Assert
        user.AuditLog.Should().NotBeNull().And.BeEmpty();
        user.UserNotifications.Should().NotBeNull().And.BeEmpty();
        user.WeightLogs.Should().NotBeNull().And.BeEmpty();
        user.UserPreferences.Should().BeNull(); // One-to-one relationship
    }

    [Fact]
    public void Users_ShouldSetAndGetUserPreferences()
    {
        // Arrange
        var user = new Users();
        var preferences = new UserPreferences();

        // Act
        user.UserPreferences = preferences;

        // Assert
        user.UserPreferences.Should().Be(preferences);
    }
}
