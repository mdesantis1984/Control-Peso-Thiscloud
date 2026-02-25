using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using Moq;

namespace ControlPeso.Application.Tests.Validators;

public sealed class CreateWeightLogValidatorTests
{
    private readonly CreateWeightLogValidator _validator;

    public CreateWeightLogValidatorTests()
    {
        // Mock IStringLocalizer - devuelve el key como valor para tests unitarios
        // Tests de traducción están en Integration/CreateWeightLogValidatorLocalizationTests.cs
        var mockLocalizer = new Mock<IStringLocalizer<CreateWeightLogValidator>>();
        mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        mockLocalizer
            .Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, key)); // NO formatear - devolver clave

        _validator = new CreateWeightLogValidator(mockLocalizer.Object);
    }

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
            .WithErrorMessage("UserIdRequired");
    }

    // NOTE: Future date validation removed from validator (UI/UX concern, not domain rule).
    // Test removed: Date_Future_ShouldFail

    [Fact]
    public void Date_Valid_ShouldPass()
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
            .WithErrorMessage("WeightMinimum");
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
            .WithErrorMessage("WeightMaximum");
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
            .WithErrorMessage("DisplayUnitInvalid");
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
            .WithErrorMessage("NoteMaxLength");
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
