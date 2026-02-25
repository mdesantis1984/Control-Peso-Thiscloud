using ControlPeso.Application.DTOs;
using ControlPeso.Application.Validators;
using ControlPeso.Domain.Enums;
using FluentValidation.TestHelper;

namespace ControlPeso.Application.Tests.Validators.Integration;

/// <summary>
/// Tests de integración para verificar traducciones de UpdateUserProfileValidator.
/// Valida que los recursos .resx existen y se cargan correctamente para es-AR y en-US.
/// </summary>
public sealed class UpdateUserProfileValidatorLocalizationTests : ValidatorLocalizationTestsBase
{
    [Fact]
    public void Localization_EsAR_AllTranslationsExist()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateUserProfileValidator>("es-AR");

        // Assert - Verificar que todas las claves existen y no están vacías
        AssertTranslationExists(localizer, "NameRequired", "es-AR");
        AssertTranslationExistsWithArgs(localizer, "NameMinLength", "es-AR", 1);
        AssertTranslationExistsWithArgs(localizer, "NameMaxLength", "es-AR", 200);
        AssertTranslationExistsWithArgs(localizer, "HeightMinimum", "es-AR", 50);
        AssertTranslationExistsWithArgs(localizer, "HeightMaximum", "es-AR", 300);
        AssertTranslationExists(localizer, "UnitSystemInvalid", "es-AR");
        AssertTranslationExists(localizer, "DateOfBirthCannotBeFuture", "es-AR");
        AssertTranslationExistsWithArgs(localizer, "DateOfBirthTooOld", "es-AR", 150);
        AssertTranslationExists(localizer, "LanguageRequired", "es-AR");
        AssertTranslationExists(localizer, "LanguageInvalid", "es-AR");
        AssertTranslationExistsWithArgs(localizer, "GoalWeightMinimum", "es-AR", 20);
        AssertTranslationExistsWithArgs(localizer, "GoalWeightMaximum", "es-AR", 500);
    }

    [Fact]
    public void Localization_EnUS_AllTranslationsExist()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateUserProfileValidator>("en-US");

        // Assert - Verificar que todas las claves existen y no están vacías
        AssertTranslationExists(localizer, "NameRequired", "en-US");
        AssertTranslationExistsWithArgs(localizer, "NameMinLength", "en-US", 1);
        AssertTranslationExistsWithArgs(localizer, "NameMaxLength", "en-US", 200);
        AssertTranslationExistsWithArgs(localizer, "HeightMinimum", "en-US", 50);
        AssertTranslationExistsWithArgs(localizer, "HeightMaximum", "en-US", 300);
        AssertTranslationExists(localizer, "UnitSystemInvalid", "en-US");
        AssertTranslationExists(localizer, "DateOfBirthCannotBeFuture", "en-US");
        AssertTranslationExistsWithArgs(localizer, "DateOfBirthTooOld", "en-US", 150);
        AssertTranslationExists(localizer, "LanguageRequired", "en-US");
        AssertTranslationExists(localizer, "LanguageInvalid", "en-US");
        AssertTranslationExistsWithArgs(localizer, "GoalWeightMinimum", "en-US", 20);
        AssertTranslationExistsWithArgs(localizer, "GoalWeightMaximum", "en-US", 500);
    }

    [Fact]
    public void Validator_WithEsARCulture_ReturnsTranslatedMessages()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateUserProfileValidator>("es-AR");
        var validator = new UpdateUserProfileValidator(localizer);

        var dto = new UpdateUserProfileDto
        {
            Name = "", // Empty (required)
            Height = 10m, // Too low
            UnitSystem = (UnitSystem)999, // Invalid enum
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), // Future
            Language = "fr", // Invalid (solo es/en)
            GoalWeight = 10m // Too low
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert - Verificar que devuelve mensajes traducidos (NO claves)
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Height);
        result.ShouldHaveValidationErrorFor(x => x.UnitSystem);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        result.ShouldHaveValidationErrorFor(x => x.Language);
        result.ShouldHaveValidationErrorFor(x => x.GoalWeight);

        // Verificar que NO son las claves (son mensajes traducidos)
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        Assert.DoesNotContain("NameRequired", errors);
        Assert.DoesNotContain("HeightMinimum", errors);
        Assert.DoesNotContain("UnitSystemInvalid", errors);
        Assert.DoesNotContain("DateOfBirthCannotBeFuture", errors);
        Assert.DoesNotContain("LanguageInvalid", errors);
        Assert.DoesNotContain("GoalWeightMinimum", errors);

        // Verificar que contienen texto real en español
        Assert.Contains(errors, e => e.Contains("requerido") || e.Contains("debe") || e.Contains("no puede"));
    }

    [Fact]
    public void Validator_WithEnUSCulture_ReturnsTranslatedMessages()
    {
        // Arrange
        var localizer = CreateLocalizer<UpdateUserProfileValidator>("en-US");
        var validator = new UpdateUserProfileValidator(localizer);

        var dto = new UpdateUserProfileDto
        {
            Name = "", // Empty (required)
            Height = 10m, // Too low
            UnitSystem = (UnitSystem)999, // Invalid enum
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), // Future
            Language = "fr", // Invalid (solo es/en)
            GoalWeight = 10m // Too low
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert - Verificar que devuelve mensajes traducidos en inglés
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Height);
        result.ShouldHaveValidationErrorFor(x => x.UnitSystem);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        result.ShouldHaveValidationErrorFor(x => x.Language);
        result.ShouldHaveValidationErrorFor(x => x.GoalWeight);

        // Verificar que NO son las claves
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        Assert.DoesNotContain("NameRequired", errors);
        Assert.DoesNotContain("HeightMinimum", errors);
        Assert.DoesNotContain("UnitSystemInvalid", errors);
        Assert.DoesNotContain("DateOfBirthCannotBeFuture", errors);
        Assert.DoesNotContain("LanguageInvalid", errors);
        Assert.DoesNotContain("GoalWeightMinimum", errors);

        // Verificar que contienen texto real en inglés
        Assert.Contains(errors, e => e.Contains("required") || e.Contains("must") || e.Contains("cannot"));
    }
}
