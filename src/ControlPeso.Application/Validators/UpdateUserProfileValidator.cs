using ControlPeso.Application.DTOs;
using FluentValidation;

namespace ControlPeso.Application.Validators;

/// <summary>
/// Validador para UpdateUserProfileDto.
/// Valida entrada de usuario al actualizar perfil (solo campos editables).
/// </summary>
public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name es requerido.")
            .MinimumLength(1)
            .WithMessage("Name debe tener al menos 1 carácter.")
            .MaximumLength(200)
            .WithMessage("Name no puede exceder 200 caracteres.");

        RuleFor(x => x.Height)
            .GreaterThanOrEqualTo(50m)
            .WithMessage("Height debe ser al menos 50 cm (rango razonable para humanos).")
            .LessThanOrEqualTo(300m)
            .WithMessage("Height no puede exceder 300 cm (rango razonable para humanos).");

        RuleFor(x => x.UnitSystem)
            .IsInEnum()
            .WithMessage("UnitSystem debe ser un valor válido (Metric o Imperial).");

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("DateOfBirth no puede ser una fecha futura.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-150)))
            .WithMessage("DateOfBirth no puede ser anterior a 150 años atrás.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language es requerido.")
            .Must(lang => lang == "es" || lang == "en")
            .WithMessage("Language debe ser 'es' o 'en'.");

        RuleFor(x => x.GoalWeight)
            .GreaterThanOrEqualTo(20m)
            .WithMessage("GoalWeight debe ser al menos 20 kg (rango razonable para humanos).")
            .LessThanOrEqualTo(500m)
            .WithMessage("GoalWeight no puede exceder 500 kg (rango razonable para humanos).")
            .When(x => x.GoalWeight.HasValue);
    }
}
