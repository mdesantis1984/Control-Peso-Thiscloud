using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;

namespace ControlPeso.Application.Tests.Validators;

public sealed class CreateWeightLogValidatorTests
{
    private readonly CreateWeightLogValidator _validator = new();

    [Fact]
    public void ValidDto_ShouldPass()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "After lunch"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserId_Empty_ShouldFail()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.Empty,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId es requerido.");
    }

    [Fact]
    public void Date_Future_ShouldFail()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("Date no puede ser una fecha futura.");
    }

    [Fact]
    public void Date_Today_ShouldPass()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(0)]
    [InlineData(-10)]
    public void Weight_LessThan20_ShouldFail(double weight)
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = (decimal)weight,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Weight debe ser al menos 20 kg (rango razonable para humanos).");
    }

    [Theory]
    [InlineData(500.1)]
    [InlineData(1000)]
    public void Weight_GreaterThan500_ShouldFail(double weight)
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = (decimal)weight,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Weight no puede exceder 500 kg (rango razonable para humanos).");
    }

    [Theory]
    [InlineData(20)]
    [InlineData(75.5)]
    [InlineData(500)]
    public void Weight_WithinRange_ShouldPass(double weight)
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = (decimal)weight,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void DisplayUnit_Invalid_ShouldFail()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = (WeightUnit)99,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayUnit)
            .WithErrorMessage("DisplayUnit debe ser un valor válido (Kg o Lb).");
    }

    [Fact]
    public void Note_TooLong_ShouldFail()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = new string('a', 501)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Note)
            .WithErrorMessage("Note no puede exceder 500 caracteres.");
    }

    [Fact]
    public void Note_Null_ShouldPass()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Note_500Characters_ShouldPass()
    {
        // Arrange
        var dto = new CreateWeightLogDto
        {
            UserId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = new string('a', 500)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }
}
