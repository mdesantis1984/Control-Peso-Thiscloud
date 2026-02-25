using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;

namespace ControlPeso.Application.Tests.Validators.Integration;

/// <summary>
/// Tests de integración para verificar traducciones de UpdateWeightLogValidator.
/// Valida que los recursos .resx existen y se cargan correctamente para es-AR y en-US.
/// </summary>
public sealed class UpdateWeightLogValidatorLocalizationTests : ValidatorLocalizationTestsBase
{
    [Fact]
    public void Localization_EsAR_AllTranslationsExist()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateWeightLogValidator>("es-AR");

        // Assert - Verificar que todas las claves existen y no están vacías
        AssertTranslationExists(localizer, "DateRequired", "es-AR");
        // NOTE: DateCannotBeFuture removed - UI/UX validation, not domain rule
        AssertTranslationExists(localizer, "TimeRequired", "es-AR");
        AssertTranslationExistsWithArgs(localizer, "WeightMinimum", "es-AR", 20);
        AssertTranslationExistsWithArgs(localizer, "WeightMaximum", "es-AR", 500);
        AssertTranslationExists(localizer, "DisplayUnitInvalid", "es-AR");
        AssertTranslationExistsWithArgs(localizer, "NoteMaxLength", "es-AR", 500);
    }

    [Fact]
    public void Localization_EnUS_AllTranslationsExist()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateWeightLogValidator>("en-US");

        // Assert - Verificar que todas las claves existen y no están vacías
        AssertTranslationExists(localizer, "DateRequired", "en-US");
        // NOTE: DateCannotBeFuture removed - UI/UX validation, not domain rule
        AssertTranslationExists(localizer, "TimeRequired", "en-US");
        AssertTranslationExistsWithArgs(localizer, "WeightMinimum", "en-US", 20);
        AssertTranslationExistsWithArgs(localizer, "WeightMaximum", "en-US", 500);
        AssertTranslationExists(localizer, "DisplayUnitInvalid", "en-US");
        AssertTranslationExistsWithArgs(localizer, "NoteMaxLength", "en-US", 500);
    }

    [Fact]
    public void Validator_WithEsARCulture_ReturnsTranslatedMessages()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateWeightLogValidator>("es-AR");
        var validator = new UpdateWeightLogValidator(localizer);

        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.MinValue, // Invalid (empty)
            Time = TimeOnly.MinValue,
            Weight = 10m, // Too low
            DisplayUnit = (WeightUnit)999, // Invalid enum
            Note = new string('x', 501) // Too long
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert - Verificar que devuelve mensajes traducidos (NO claves)
        result.ShouldHaveValidationErrorFor(x => x.Date);
        result.ShouldHaveValidationErrorFor(x => x.Weight);
        result.ShouldHaveValidationErrorFor(x => x.DisplayUnit);
        result.ShouldHaveValidationErrorFor(x => x.Note);

        // Verificar que NO son las claves (son mensajes traducidos)
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        Assert.DoesNotContain("DateRequired", errors);
        Assert.DoesNotContain("WeightMinimum", errors);
        Assert.DoesNotContain("DisplayUnitInvalid", errors);
        Assert.DoesNotContain("NoteMaxLength", errors);

        // Verificar que contienen texto real en español
        Assert.Contains(errors, e => e.Contains("requerido") || e.Contains("debe") || e.Contains("no puede"));
    }

    [Fact]
    public void Validator_WithEnUSCulture_ReturnsTranslatedMessages()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateWeightLogValidator>("en-US");
        var validator = new UpdateWeightLogValidator(localizer);

        var dto = new UpdateWeightLogDto
        {
            Date = DateOnly.MinValue, // Invalid (empty)
            Time = TimeOnly.MinValue,
            Weight = 10m, // Too low
            DisplayUnit = (WeightUnit)999, // Invalid enum
            Note = new string('x', 501) // Too long
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert - Verificar que devuelve mensajes traducidos en inglés
        result.ShouldHaveValidationErrorFor(x => x.Date);
        result.ShouldHaveValidationErrorFor(x => x.Weight);
        result.ShouldHaveValidationErrorFor(x => x.DisplayUnit);
        result.ShouldHaveValidationErrorFor(x => x.Note);

        // Verificar que NO son las claves
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        Assert.DoesNotContain("DateRequired", errors);
        Assert.DoesNotContain("WeightMinimum", errors);
        Assert.DoesNotContain("DisplayUnitInvalid", errors);
        Assert.DoesNotContain("NoteMaxLength", errors);

        // Verificar que contienen texto real en inglés
        Assert.Contains(errors, e => e.Contains("required") || e.Contains("must") || e.Contains("cannot"));
    }
}
