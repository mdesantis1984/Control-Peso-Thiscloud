using ControlPeso.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace ControlPeso.Application.Validators;

/// <summary>
/// Validador para UpdateWeightLogDto.
/// Valida entrada de usuario al actualizar un registro de peso existente.
/// </summary>
public sealed class UpdateWeightLogValidator : AbstractValidator<UpdateWeightLogDto>
{
    private readonly IStringLocalizer<UpdateWeightLogValidator> _localizer;

    public UpdateWeightLogValidator(IStringLocalizer<UpdateWeightLogValidator> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        _localizer = localizer;

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage(_localizer["DateRequired"])
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(_localizer["DateCannotBeFuture"]);

        RuleFor(x => x.Time)
            .NotEmpty()
            .WithMessage(_localizer["TimeRequired"]);

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(20m)
            .WithMessage(_localizer["WeightMinimum", 20])
            .LessThanOrEqualTo(500m)
            .WithMessage(_localizer["WeightMaximum", 500]);

        RuleFor(x => x.DisplayUnit)
            .IsInEnum()
            .WithMessage(_localizer["DisplayUnitInvalid"]);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage(_localizer["NoteMaxLength", 500]);
    }
}
