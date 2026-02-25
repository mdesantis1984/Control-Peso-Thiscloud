using ControlPeso.Application.DTOs;
using FluentAssertions;

namespace ControlPeso.Application.Tests.DTOs;

/// <summary>
/// Simple tests for AdminDashboard and AuditLog DTOs to increase coverage.
/// </summary>
public sealed class SimpleApplicationDtosTests
{
    [Fact]
    public void AdminDashboardDto_CanBeInstantiated()
    {
        // Arrange & Act
        var dto = new AdminDashboardDto
        {
            TotalUsers = 10,
            ActiveUsers = 8,
            PendingUsers = 1,
            InactiveUsers = 1,
            TotalWeightLogs = 50,
            WeightLogsLastWeek = 10,
            WeightLogsLastMonth = 25,
            LatestUserRegistration = DateTime.UtcNow
        };

        // Assert
        dto.TotalUsers.Should().Be(10);
        dto.ActiveUsers.Should().Be(8);
        dto.WeightLogsLastWeek.Should().Be(10);
    }

    [Fact]
    public void AuditLogDto_CanBeInstantiated()
    {
        // Arrange & Act
        var dto = new AuditLogDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "Test",
            EntityType = "User",
            EntityId = Guid.NewGuid(),
            OldValue = "old",
            NewValue = "new",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        dto.Action.Should().Be("Test");
        dto.EntityType.Should().Be("User");
    }

    [Fact]
    public void CreateWeightLogDto_CanBeInstantiated()
    {
        // Arrange & Act
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Weight = 75.0m,
            DisplayUnit = Domain.Enums.WeightUnit.Kg,
            Note = "test"
        };

        // Assert
        dto.Weight.Should().Be(75.0m);
        dto.Note.Should().Be("test");
    }

    [Fact]
    public void UpdateWeightLogDto_CanBeInstantiated()
    {
        // Arrange & Act
        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Weight = 76.5m,
            DisplayUnit = Domain.Enums.WeightUnit.Kg,
            Note = null
        };

        // Assert
        dto.Weight.Should().Be(76.5m);
        dto.Note.Should().BeNull();
    }

    [Fact]
    public void UpdateUserProfileDto_CanBeInstantiated()
    {
        // Arrange & Act
        var dto = new UpdateUserProfileDto
        {
            Name = "Updated Name",
            Height = 180.0m,
            UnitSystem = Domain.Enums.UnitSystem.Metric,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Language = "en",
            GoalWeight = 70.0m
        };

        // Assert
        dto.Name.Should().Be("Updated Name");
        dto.Height.Should().Be(180.0m);
        dto.Language.Should().Be("en");
    }
}
