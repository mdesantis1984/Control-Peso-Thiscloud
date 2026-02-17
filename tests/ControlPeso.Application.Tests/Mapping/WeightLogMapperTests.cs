using ControlPeso.Application.DTOs;
using ControlPeso.Application.Mapping;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;

namespace ControlPeso.Application.Tests.Mapping;

public sealed class WeightLogMapperTests
{
    [Fact]
    public void ToDto_WithValidEntity_ShouldConvertCorrectly()
    {
        // Arrange
        var entity = new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            Date = "2026-02-17",
            Time = "14:30",
            Weight = 75.5,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = "After lunch",
            Trend = (int)WeightTrend.Down,
            CreatedAt = "2026-02-17T14:30:00.0000000Z"
        };

        // Act
        var dto = WeightLogMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(Guid.Parse(entity.Id));
        dto.UserId.Should().Be(Guid.Parse(entity.UserId));
        dto.Date.Should().Be(new DateOnly(2026, 2, 17));
        dto.Time.Should().Be(new TimeOnly(14, 30));
        dto.Weight.Should().Be(75.5m);
        dto.DisplayUnit.Should().Be(WeightUnit.Kg);
        dto.Note.Should().Be("After lunch");
        dto.Trend.Should().Be(WeightTrend.Down);
        dto.CreatedAt.Should().Be(DateTime.Parse("2026-02-17T14:30:00.0000000Z"));
    }

    [Fact]
    public void ToDto_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        WeightLogs? entity = null;

        // Act
        var act = () => WeightLogMapper.ToDto(entity!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithNullNote_ShouldReturnNullNote()
    {
        // Arrange
        var entity = new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            Date = "2026-02-17",
            Time = "14:30",
            Weight = 75.5,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = null,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = "2026-02-17T14:30:00.0000000Z"
        };

        // Act
        var dto = WeightLogMapper.ToDto(entity);

        // Assert
        dto.Note.Should().BeNull();
    }

    [Fact]
    public void ToEntity_WithValidDto_ShouldCreateEntityWithDefaults()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 2, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "Test note"
        };

        // Act
        var entity = WeightLogMapper.ToEntity(dto);

        // Assert
        Guid.TryParse(entity.Id, out var id).Should().BeTrue();
        id.Should().NotBeEmpty();
        entity.UserId.Should().Be(dto.UserId.ToString());
        entity.Date.Should().Be("2026-02-17");
        entity.Time.Should().Be("14:30");
        entity.Weight.Should().Be(75.5);
        entity.DisplayUnit.Should().Be((int)WeightUnit.Kg);
        entity.Note.Should().Be("Test note");
        entity.Trend.Should().Be((int)WeightTrend.Neutral);
        DateTime.TryParse(entity.CreatedAt, out var createdAt).Should().BeTrue();
        createdAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
    }

    [Fact]
    public void ToEntity_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        CreateWeightLogDto? dto = null;

        // Act
        var act = () => WeightLogMapper.ToEntity(dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(WeightUnit.Kg, 0)]
    [InlineData(WeightUnit.Lb, 1)]
    public void ToEntity_WithDifferentDisplayUnits_ShouldMapCorrectly(WeightUnit unit, int expectedValue)
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 2, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = unit,
            Note = null
        };

        // Act
        var entity = WeightLogMapper.ToEntity(dto);

        // Assert
        entity.DisplayUnit.Should().Be(expectedValue);
    }

    [Fact]
    public void UpdateEntity_WithValidData_ShouldUpdateAllEditableFields()
    {
        // Arrange
        var existingEntity = new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            Date = "2026-02-16",
            Time = "10:00",
            Weight = 76.0,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = "Old note",
            Trend = (int)WeightTrend.Up,
            CreatedAt = "2026-02-16T10:00:00.0000000Z"
        };

        var updateDto = new UpdateWeightLogDto
        {
            Date = new DateOnly(2026, 2, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Lb,
            Note = "Updated note"
        };

        // Act
        WeightLogMapper.UpdateEntity(existingEntity, updateDto);

        // Assert
        existingEntity.Date.Should().Be("2026-02-17");
        existingEntity.Time.Should().Be("14:30");
        existingEntity.Weight.Should().Be(75.5);
        existingEntity.DisplayUnit.Should().Be((int)WeightUnit.Lb);
        existingEntity.Note.Should().Be("Updated note");
        // Should NOT modify these fields
        existingEntity.Id.Should().NotBeEmpty();
        existingEntity.UserId.Should().NotBeEmpty();
        existingEntity.CreatedAt.Should().Be("2026-02-16T10:00:00.0000000Z");
    }

    [Fact]
    public void UpdateEntity_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        WeightLogs? entity = null;
        var dto = new UpdateWeightLogDto
        {
            Date = new DateOnly(2026, 2, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var act = () => WeightLogMapper.UpdateEntity(entity!, dto);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateEntity_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var entity = new WeightLogs
        {
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            Date = "2026-02-17",
            Time = "14:30",
            Weight = 75.5,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = null,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = "2026-02-17T14:30:00.0000000Z"
        };
        UpdateWeightLogDto? dto = null;

        // Act
        var act = () => WeightLogMapper.UpdateEntity(entity, dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
