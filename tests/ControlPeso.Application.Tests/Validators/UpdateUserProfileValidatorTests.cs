using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;

namespace ControlPeso.Application.Tests.Validators;

public sealed class UpdateUserProfileValidatorTests
{
    private readonly UpdateUserProfileValidator _validator = new();

    [Fact]
    public void ValidDto_ShouldPass()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = new DateOnly(1990, 5, 15),
            Language = "es",
            GoalWeight = 70m
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_EmptyOrNull_ShouldFail(string? name)
    {
        var dto = new UpdateUserProfileDto
        {
            Name = name!,
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = new string('a', 201),
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(49.9)]
    [InlineData(300.1)]
    public void Height_OutOfRange_ShouldFail(double height)
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = (decimal)height,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Height);
    }

    [Fact]
    public void DateOfBirth_Future_ShouldFail()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void DateOfBirth_TooOld_ShouldFail()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-151)),
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void DateOfBirth_Null_ShouldPass()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData(null)]
    public void Language_Invalid_ShouldFail(string? language)
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = language!,
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Language);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void Language_Valid_ShouldPass(string language)
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = language,
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Language);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(500.1)]
    public void GoalWeight_OutOfRange_ShouldFail(double weight)
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = (decimal)weight
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.GoalWeight);
    }

    [Fact]
    public void GoalWeight_Null_ShouldPass()
    {
        var dto = new UpdateUserProfileDto
        {
            Name = "Test User",
            Height = 175m,
            UnitSystem = UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            GoalWeight = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.GoalWeight);
    }
}
