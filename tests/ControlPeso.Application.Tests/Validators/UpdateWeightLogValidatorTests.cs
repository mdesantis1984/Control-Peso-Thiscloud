using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using Moq;

namespace ControlPeso.Application.Tests.Validators;

public sealed class UpdateWeightLogValidatorTests
{
    private readonly UpdateWeightLogValidator _validator;

    public UpdateWeightLogValidatorTests()
    {
        // Mock IStringLocalizer - devuelve el key como valor para tests
        var mockLocalizer = new Mock<IStringLocalizer<UpdateWeightLogValidator>>();
        mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        mockLocalizer
            .Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

        _validator = new UpdateWeightLogValidator(mockLocalizer.Object);
    }

    [Fact]
    public void ValidDto_ShouldPass()
    {
        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = "After lunch"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Date_Future_ShouldFail()
    {
        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(500.1)]
    public void Weight_OutOfRange_ShouldFail(double weight)
    {
        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = (decimal)weight,
            DisplayUnit = WeightUnit.Kg,
            Note = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Fact]
    public void Note_TooLong_ShouldFail()
    {
        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(14, 30),
            Weight = 75.5m,
            DisplayUnit = WeightUnit.Kg,
            Note = new string('a', 501)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
