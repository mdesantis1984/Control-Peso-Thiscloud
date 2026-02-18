using ControlPeso.Application.DTOs;
using FluentValidation;

namespace ControlPeso.Application.Validators;

/// <summary>
/// Validador para CreateWeightLogDto.
/// Valida entrada de usuario al crear un nuevo registro de peso.
/// </summary>
public sealed class CreateWeightLogValidator : AbstractValidator<CreateWeightLogDto>
{
    public CreateWeightLogValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId es requerido.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date es requerida.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date no puede ser una fecha futura.");

        RuleFor(x => x.Time)
            .NotEmpty()
            .WithMessage("Time es requerido.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(20m)
            .WithMessage("Weight debe ser al menos 20 kg (rango razonable para humanos).")
            .LessThanOrEqualTo(500m)
            .WithMessage("Weight no puede exceder 500 kg (rango razonable para humanos).");

        RuleFor(x => x.DisplayUnit)
            .IsInEnum()
            .WithMessage("DisplayUnit debe ser un valor válido (Kg o Lb).");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note no puede exceder 500 caracteres.");
    }
}
