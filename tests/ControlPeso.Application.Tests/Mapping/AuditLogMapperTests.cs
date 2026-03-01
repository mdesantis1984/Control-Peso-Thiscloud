using ControlPeso.Application.Mapping;
using ControlPeso.Domain.Entities;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Mapping;

public sealed class AuditLogMapperTests
{
    [Fact]
    public void ToDto_WithValidEntity_ShouldConvertCorrectly()
    {
        // Arrange
        var entity = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "UserRoleChanged",
            EntityType = "User",
            EntityId = Guid.NewGuid().ToString(),
            OldValue = "{\"Role\":0}",
            NewValue = "{\"Role\":1}",
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
        };

        // Act
        var dto = AuditLogMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.UserId.Should().Be(entity.UserId);
        dto.Action.Should().Be("UserRoleChanged");
        dto.EntityType.Should().Be("User");
        dto.EntityId.Should().Be(entity.EntityId);
        dto.OldValue.Should().Be("{\"Role\":0}");
        dto.NewValue.Should().Be("{\"Role\":1}");
        dto.CreatedAt.Should().Be(DateTime.Parse("2026-02-17T14:30:00.0000000Z"));
    }

    [Fact]
    public void ToDto_WithNullOldValue_ShouldHandleCorrectly()
    {
        // Arrange
        var entity = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "UserCreated",
            EntityType = "User",
            EntityId = Guid.NewGuid().ToString(),
            OldValue = null,
            NewValue = "{\"Name\":\"New User\"}",
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
        };

        // Act
        var dto = AuditLogMapper.ToDto(entity);

        // Assert
        dto.OldValue.Should().BeNull();
        dto.NewValue.Should().Be("{\"Name\":\"New User\"}");
    }

    [Fact]
    public void ToDto_WithNullNewValue_ShouldHandleCorrectly()
    {
        // Arrange
        var entity = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "UserDeleted",
            EntityType = "User",
            EntityId = Guid.NewGuid().ToString(),
            OldValue = "{\"Name\":\"Deleted User\"}",
            NewValue = null,
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
        };

        // Act
        var dto = AuditLogMapper.ToDto(entity);

        // Assert
        dto.OldValue.Should().Be("{\"Name\":\"Deleted User\"}");
        dto.NewValue.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        AuditLog? entity = null;

        // Act
        var act = () => AuditLogMapper.ToDto(entity!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateEntity_WithValidData_ShouldCreateEntityWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "UserRoleChanged";
        var entityType = "User";
        var entityId = Guid.NewGuid();
        var oldValue = "{\"Role\":0}";
        var newValue = "{\"Role\":1}";

        // Act
        var entity = AuditLogMapper.CreateEntity(
            userId,
            action,
            entityType,
            entityId,
            oldValue,
            newValue);

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.UserId.Should().Be(userId);
        entity.Action.Should().Be(action);
        entity.EntityType.Should().Be(entityType);
        entity.EntityId.Should().Be(entityId.ToString());
        entity.OldValue.Should().Be(oldValue);
        entity.NewValue.Should().Be(newValue);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
    }

    [Fact]
    public void CreateEntity_WithNullOldValue_ShouldCreateEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "UserCreated";
        var entityType = "User";
        var entityId = Guid.NewGuid();
        string? oldValue = null;
        var newValue = "{\"Name\":\"New User\"}";

        // Act
        var entity = AuditLogMapper.CreateEntity(
            userId,
            action,
            entityType,
            entityId,
            oldValue,
            newValue);

        // Assert
        entity.OldValue.Should().BeNull();
        entity.NewValue.Should().Be(newValue);
    }

    [Fact]
    public void CreateEntity_WithNullNewValue_ShouldCreateEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "UserDeleted";
        var entityType = "User";
        var entityId = Guid.NewGuid();
        var oldValue = "{\"Name\":\"Deleted User\"}";
        string? newValue = null;

        // Act
        var entity = AuditLogMapper.CreateEntity(
            userId,
            action,
            entityType,
            entityId,
            oldValue,
            newValue);

        // Assert
        entity.OldValue.Should().Be(oldValue);
        entity.NewValue.Should().BeNull();
    }

    [Fact]
    public void CreateEntity_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        string? action = null;
        var entityType = "User";
        var entityId = Guid.NewGuid();
        var oldValue = "{\"Role\":0}";
        var newValue = "{\"Role\":1}";

        // Act
        var act = () => AuditLogMapper.CreateEntity(
            userId,
            action!,
            entityType,
            entityId,
            oldValue,
            newValue);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateEntity_WithNullEntityType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "UserRoleChanged";
        string? entityType = null;
        var entityId = Guid.NewGuid();
        var oldValue = "{\"Role\":0}";
        var newValue = "{\"Role\":1}";

        // Act
        var act = () => AuditLogMapper.CreateEntity(
            userId,
            action,
            entityType!,
            entityId,
            oldValue,
            newValue);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
