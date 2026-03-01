using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// EditWeightDialog - Diálogo para editar un registro de peso existente
/// Formulario con fecha, hora, peso, nota opcional y validación
/// </summary>
public partial class EditWeightDialog
{
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    /// <summary>
    /// DTO del registro a editar (pasado desde el componente padre)
    /// </summary>
    [Parameter, EditorRequired]
    public WeightLogDto WeightLog { get; set; } = null!;

    [Inject] private IStringLocalizer<EditWeightDialog> Localizer { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private ILogger<EditWeightDialog> Logger { get; set; } = null!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = null!;
    [Inject] private Services.UserStateService UserStateService { get; set; } = null!;

    private MudForm? _form;
    private bool _isValid;
    private bool _isSubmitting;
    private bool _isKg = true; // Toggle para kg/lb

    private DateTime? _date;
    private TimeSpan? _time;
    private string _weightText = string.Empty;
    private string _note = string.Empty;

    // Localized Properties
    private string DialogTitle => Localizer[nameof(DialogTitle)];
    private string DialogSubtitle => Localizer[nameof(DialogSubtitle)];
    private string WeightLabel => Localizer[nameof(WeightLabel)];
    private string ToggleUnitAriaLabel => Localizer[nameof(ToggleUnitAriaLabel)];
    private string UnitHelperText => Localizer[nameof(UnitHelperText)];
    private string WeightPlaceholder => Localizer[nameof(WeightPlaceholder)];
    private string DateLabel => Localizer[nameof(DateLabel)];
    private string TimeLabel => Localizer[nameof(TimeLabel)];
    private string NotesLabel => Localizer[nameof(NotesLabel)];
    private string NotesPlaceholder => Localizer[nameof(NotesPlaceholder)];
    private string NotesHelperText => Localizer[nameof(NotesHelperText)];
    private string InfoMessage => Localizer[nameof(InfoMessage)];
    private string CancelButton => Localizer[nameof(CancelButton)];
    private string SaveButton => Localizer[nameof(SaveButton)];
    private string SuccessMessage => Localizer[nameof(SuccessMessage)];
    private string ErrorDateTimeRequired => Localizer[nameof(ErrorDateTimeRequired)];
    private string ErrorInvalidWeight => Localizer[nameof(ErrorInvalidWeight)];
    private string GetErrorSaving(string message) => Localizer["ErrorSaving", message];

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(WeightLog);

        // Initialize unit toggle from user's global preference
        _isKg = UserStateService.CurrentUnitSystem == UnitSystem.Metric;

        // Convert stored weight (always in kg) to user's preferred unit for display
        var displayWeight = _isKg 
            ? WeightLog.Weight 
            : WeightLog.Weight * 2.20462m; // kg → lb

        // Initialize form fields from WeightLog
        _date = WeightLog.Date.ToDateTime(TimeOnly.MinValue);
        _time = WeightLog.Time.ToTimeSpan();
        _weightText = displayWeight.ToString("F1");
        _note = WeightLog.Note ?? string.Empty;

        Logger.LogInformation(
            "EditWeightDialog: Initialized for weight log {WeightLogId} - Weight: {Weight}kg, Date: {Date}",
            WeightLog.Id, WeightLog.Weight, WeightLog.Date);
    }

    /// <summary>
    /// Toggle entre kg y lb cuando se hace clic en el Adornment.
    /// Convierte automáticamente el valor mostrado.
    /// </summary>
    private void ToggleUnit()
    {
        // Parse current weight value
        var normalizedWeight = _weightText.Replace(',', '.').Trim();
        if (decimal.TryParse(normalizedWeight, 
            System.Globalization.NumberStyles.AllowDecimalPoint, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out var currentWeight))
        {
            // Convert: kg → lb or lb → kg
            var newWeight = _isKg 
                ? currentWeight * 2.20462m  // kg → lb
                : currentWeight / 2.20462m; // lb → kg

            _weightText = newWeight.ToString("F1");
        }

        _isKg = !_isKg;
        Logger.LogInformation("EditWeightDialog: Unit toggled to {Unit}", _isKg ? "kg" : "lb");
    }

    private async Task Submit()
    {
        if (_form == null || !_isValid || _isSubmitting)
        {
            Logger.LogWarning("EditWeightDialog: Submit called but form invalid or already submitting");
            return;
        }

        _isSubmitting = true;

        try
        {
            if (!_date.HasValue || !_time.HasValue)
            {
                Logger.LogWarning("EditWeightDialog: Date or time not set");
                Snackbar.Add(ErrorDateTimeRequired, Severity.Error);
                return;
            }

            // Parse weight from text field (accept both comma and dot as decimal separator)
            var normalizedWeight = _weightText.Replace(',', '.').Trim();
            if (!decimal.TryParse(normalizedWeight, 
                System.Globalization.NumberStyles.AllowDecimalPoint | 
                System.Globalization.NumberStyles.AllowLeadingWhite | 
                System.Globalization.NumberStyles.AllowTrailingWhite, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out var weightValue) || weightValue < 20 || weightValue > 500)
            {
                Logger.LogWarning("EditWeightDialog: Invalid weight value - Input: {WeightText}", _weightText);
                Snackbar.Add(ErrorInvalidWeight, Severity.Error);
                return;
            }

            // Convert lb → kg if user entered in Imperial units (storage is always in kg)
            var weightInKg = _isKg ? weightValue : weightValue / 2.20462m;

            var dto = new UpdateWeightLogDto
            {
                Date = DateOnly.FromDateTime(_date.Value),
                Time = TimeOnly.FromTimeSpan(_time.Value),
                Weight = weightInKg, // Always store in kg
                DisplayUnit = _isKg ? WeightUnit.Kg : WeightUnit.Lb,
                Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim()
            };

            Logger.LogInformation(
                "EditWeightDialog: Updating weight log {WeightLogId} - Date: {Date}, Weight: {Weight}kg (input: {Input} {Unit})",
                WeightLog.Id, dto.Date, dto.Weight, weightValue, _isKg ? "kg" : "lb");

            await WeightLogService.UpdateAsync(WeightLog.Id, dto);

            Logger.LogInformation("EditWeightDialog: Weight log updated successfully");
            Snackbar.Add(SuccessMessage, Severity.Success);
            MudDialog?.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "EditWeightDialog: Error updating weight log {WeightLogId}", WeightLog.Id);
            Snackbar.Add(GetErrorSaving(ex.Message), Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        Logger.LogInformation("EditWeightDialog: Cancelled");
        MudDialog?.Close(DialogResult.Cancel());
    }
}
