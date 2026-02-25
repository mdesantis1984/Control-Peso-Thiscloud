using ControlPeso.Domain.Entities;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Entities;

public class UserPreferencesTests
{
    [Fact]
    public void UserPreferences_ShouldSetAndGetId()
    {
        var prefs = new UserPreferences();
        const string id = "pref-123";

        prefs.Id = id;

        prefs.Id.Should().Be(id);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetUserId()
    {
        var prefs = new UserPreferences();
        const string userId = "user-123";

        prefs.UserId = userId;

        prefs.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetDarkMode()
    {
        var prefs = new UserPreferences();
        const int darkMode = 1;

        prefs.DarkMode = darkMode;

        prefs.DarkMode.Should().Be(darkMode);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetNotificationsEnabled()
    {
        var prefs = new UserPreferences();
        const int notificationsEnabled = 0;

        prefs.NotificationsEnabled = notificationsEnabled;

        prefs.NotificationsEnabled.Should().Be(notificationsEnabled);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetTimeZone()
    {
        var prefs = new UserPreferences();
        const string timeZone = "America/Argentina/Buenos_Aires";

        prefs.TimeZone = timeZone;

        prefs.TimeZone.Should().Be(timeZone);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetUpdatedAt()
    {
        var prefs = new UserPreferences();
        const string updatedAt = "2024-01-15T00:00:00Z";

        prefs.UpdatedAt = updatedAt;

        prefs.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void UserPreferences_ShouldSetAndGetUser()
    {
        var prefs = new UserPreferences();
        var user = new Users();

        prefs.User = user;

        prefs.User.Should().Be(user);
    }
}

public class UserNotificationsTests
{
    [Fact]
    public void UserNotifications_ShouldSetAndGetId()
    {
        var notification = new UserNotifications();
        const string id = "notif-123";

        notification.Id = id;

        notification.Id.Should().Be(id);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetUserId()
    {
        var notification = new UserNotifications();
        const string userId = "user-123";

        notification.UserId = userId;

        notification.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetTitle()
    {
        var notification = new UserNotifications();
        const string title = "Test Notification";

        notification.Title = title;

        notification.Title.Should().Be(title);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetMessage()
    {
        var notification = new UserNotifications();
        const string message = "This is a test message";

        notification.Message = message;

        notification.Message.Should().Be(message);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetType()
    {
        var notification = new UserNotifications();
        const int type = 0;

        notification.Type = type;

        notification.Type.Should().Be(type);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetIsRead()
    {
        var notification = new UserNotifications();
        const int isRead = 1;

        notification.IsRead = isRead;

        notification.IsRead.Should().Be(isRead);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetReadAt()
    {
        var notification = new UserNotifications();
        const string readAt = "2024-01-15T10:00:00Z";

        notification.ReadAt = readAt;

        notification.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetCreatedAt()
    {
        var notification = new UserNotifications();
        const string createdAt = "2024-01-15T00:00:00Z";

        notification.CreatedAt = createdAt;

        notification.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UserNotifications_ShouldSetAndGetUser()
    {
        var notification = new UserNotifications();
        var user = new Users();

        notification.User = user;

        notification.User.Should().Be(user);
    }
}

public class AuditLogTests
{
    [Fact]
    public void AuditLog_ShouldSetAndGetId()
    {
        var log = new AuditLog();
        const string id = "audit-123";

        log.Id = id;

        log.Id.Should().Be(id);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetUserId()
    {
        var log = new AuditLog();
        const string userId = "user-123";

        log.UserId = userId;

        log.UserId.Should().Be(userId);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetAction()
    {
        var log = new AuditLog();
        const string action = "CREATE";

        log.Action = action;

        log.Action.Should().Be(action);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetEntityType()
    {
        var log = new AuditLog();
        const string entityType = "WeightLog";

        log.EntityType = entityType;

        log.EntityType.Should().Be(entityType);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetEntityId()
    {
        var log = new AuditLog();
        const string entityId = "entity-123";

        log.EntityId = entityId;

        log.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetOldValue()
    {
        var log = new AuditLog();
        const string oldValue = "{\"weight\":75.5}";

        log.OldValue = oldValue;

        log.OldValue.Should().Be(oldValue);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetNewValue()
    {
        var log = new AuditLog();
        const string newValue = "{\"weight\":76.0}";

        log.NewValue = newValue;

        log.NewValue.Should().Be(newValue);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetCreatedAt()
    {
        var log = new AuditLog();
        const string createdAt = "2024-01-15T00:00:00Z";

        log.CreatedAt = createdAt;

        log.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AuditLog_ShouldSetAndGetUser()
    {
        var log = new AuditLog();
        var user = new Users();

        log.User = user;

        log.User.Should().Be(user);
    }
}
