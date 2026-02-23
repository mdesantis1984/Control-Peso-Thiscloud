using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

/// <summary>
/// AddWeightDialog - Diálogo para registrar un nuevo peso
/// Formulario con fecha, hora, peso, nota opcional y validación
/// </summary>
public partial class AddWeightDialog
{
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Inject] private IStringLocalizer<AddWeightDialog> Localizer { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ILogger<AddWeightDialog> Logger { get; set; } = null!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = null!; // User notification service con verificación de preferencias

    private MudForm? _form;
    private bool _isValid;
    private bool _isSubmitting;
    private bool _isKg = true; // Toggle para kg/lb

    private DateTime? _date = DateTime.Today;
    private TimeSpan? _time = DateTime.Now.TimeOfDay;
    private decimal _weight = 70;
    private string _weightText = "70.0"; // Texto del campo de peso
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
    private string ErrorInvalidUser => Localizer[nameof(ErrorInvalidUser)];
    private string ErrorDateTimeRequired => Localizer[nameof(ErrorDateTimeRequired)];
    private string ErrorInvalidWeight => Localizer[nameof(ErrorInvalidWeight)];
    private string GetErrorSaving(string message) => Localizer["ErrorSaving", message];

    protected override void OnInitialized()
    {
        Logger.LogInformation("AddWeightDialog: Initialized");
    }

    /// <summary>
    /// Toggle entre kg y lb cuando se hace clic en el Adornment
    /// </summary>
    private void ToggleUnit()
    {
        _isKg = !_isKg;
        Logger.LogInformation("AddWeightDialog: Unit toggled to {Unit}", _isKg ? "kg" : "lb");
    }

    private async Task Submit()
    {
        if (_form == null || !_isValid || _isSubmitting)
        {
            Logger.LogWarning("AddWeightDialog: Submit called but form invalid or already submitting");
            return;
        }

        _isSubmitting = true;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();

            // CORRECCIÓN: Usar ClaimTypes.NameIdentifier (contiene UserId GUID después de UserClaimsTransformation)
            var userIdClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Logger.LogWarning("AddWeightDialog: Invalid user ID - Claim value: {UserIdClaim}", userIdClaim ?? "(null)");
                Snackbar.Add(ErrorInvalidUser, Severity.Error);
                return;
            }

            if (!_date.HasValue || !_time.HasValue)
            {
                Logger.LogWarning("AddWeightDialog: Date or time not set");
                Snackbar.Add(ErrorDateTimeRequired, Severity.Error);
                return;
            }

            // Parse weight from text field
            if (!decimal.TryParse(_weightText, out var weightValue) || weightValue < 20 || weightValue > 500)
            {
                Logger.LogWarning("AddWeightDialog: Invalid weight value - Input: {WeightText}", _weightText);
                Snackbar.Add(ErrorInvalidWeight, Severity.Error);
                return;
            }

            var dto = new CreateWeightLogDto
            {
                UserId = userId,
                Date = DateOnly.FromDateTime(_date.Value),
                Time = TimeOnly.FromTimeSpan(_time.Value),
                Weight = weightValue,
                DisplayUnit = _isKg ? WeightUnit.Kg : WeightUnit.Lb,
                Note = string.IsNullOrWhiteSpace(_note) ? null : _note.Trim()
            };

            Logger.LogInformation("AddWeightDialog: Creating weight log - User: {UserId}, Date: {Date}, Weight: {Weight}kg",
                userId, dto.Date, dto.Weight);

            await WeightLogService.CreateAsync(dto);

            Logger.LogInformation("AddWeightDialog: Weight log created successfully");
            Snackbar.Add(SuccessMessage, Severity.Success);
            MudDialog?.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddWeightDialog: Error creating weight log");
            Snackbar.Add(GetErrorSaving(ex.Message), Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        Logger.LogInformation("AddWeightDialog: Cancelled");
        MudDialog?.Close(DialogResult.Cancel());
    }
}
