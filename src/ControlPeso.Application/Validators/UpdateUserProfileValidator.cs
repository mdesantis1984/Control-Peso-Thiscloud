using ControlPeso.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Application.Validators;

/// <summary>
/// Validador para UpdateUserProfileDto.
/// Valida entrada de usuario al actualizar perfil (solo campos editables).
/// </summary>
public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileDto>
{
    private readonly IStringLocalizer<UpdateUserProfileValidator> _localizer;

    public UpdateUserProfileValidator(IStringLocalizer<UpdateUserProfileValidator> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(_localizer["NameRequired"])
            .MinimumLength(1)
            .WithMessage(_localizer["NameMinLength", 1])
            .MaximumLength(200)
            .WithMessage(_localizer["NameMaxLength", 200]);

        RuleFor(x => x.Height)
            .GreaterThanOrEqualTo(50m)
            .WithMessage(_localizer["HeightMinimum", 50])
            .LessThanOrEqualTo(300m)
            .WithMessage(_localizer["HeightMaximum", 300]);

        RuleFor(x => x.UnitSystem)
            .IsInEnum()
            .WithMessage(_localizer["UnitSystemInvalid"]);

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(_localizer["DateOfBirthCannotBeFuture"])
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-150)))
            .WithMessage(_localizer["DateOfBirthTooOld", 150])
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage(_localizer["LanguageRequired"])
            .Must(lang => lang == "es" || lang == "en")
            .WithMessage(_localizer["LanguageInvalid"]);

        RuleFor(x => x.GoalWeight)
            .GreaterThanOrEqualTo(20m)
            .WithMessage(_localizer["GoalWeightMinimum", 20])
            .LessThanOrEqualTo(500m)
            .WithMessage(_localizer["GoalWeightMaximum", 500])
            .When(x => x.GoalWeight.HasValue);
    }
}
