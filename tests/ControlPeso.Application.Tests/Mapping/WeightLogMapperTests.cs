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
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 02, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = "After lunch",
            Trend = (int)WeightTrend.Down,
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
        };

        // Act
        var dto = WeightLogMapper.ToDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.UserId.Should().Be(entity.UserId);
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
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 02, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = null,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
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
        entity.Id.Should().NotBeEmpty();
        entity.UserId.Should().Be(dto.UserId);
        entity.Date.Should().Be(new DateOnly(2026, 2, 17));
        entity.Time.Should().Be(new TimeOnly(14, 30));
        entity.Weight.Should().Be(75.5m);
        entity.DisplayUnit.Should().Be((int)WeightUnit.Kg);
        entity.Note.Should().Be("Test note");
        entity.Trend.Should().Be((int)WeightTrend.Neutral);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(2));
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
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 02, 16),
            Time = new TimeOnly(10, 00),
            Weight = 76.0m,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = "Old note",
            Trend = (int)WeightTrend.Up,
            CreatedAt = DateTime.Parse("2026-02-16T10:00:00.0000000Z")
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
        existingEntity.Date.Should().Be(new DateOnly(2026, 2, 17));
        existingEntity.Time.Should().Be(new TimeOnly(14, 30));
        existingEntity.Weight.Should().Be(75.5m);
        existingEntity.DisplayUnit.Should().Be((int)WeightUnit.Lb);
        existingEntity.Note.Should().Be("Updated note");
        // Should NOT modify these fields
        existingEntity.Id.Should().NotBeEmpty();
        existingEntity.UserId.Should().NotBeEmpty();
        existingEntity.CreatedAt.Should().Be(DateTime.Parse("2026-02-16T10:00:00.0000000Z"));
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
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Date = new DateOnly(2026, 02, 17),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = (int)WeightUnit.Kg,
            Note = null,
            Trend = (int)WeightTrend.Neutral,
            CreatedAt = DateTime.Parse("2026-02-17T14:30:00.0000000Z")
        };
        UpdateWeightLogDto? dto = null;

        // Act
        var act = () => WeightLogMapper.UpdateEntity(entity, dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
