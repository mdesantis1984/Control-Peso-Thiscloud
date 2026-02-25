using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Security.Claims;
using AppDateRange = ControlPeso.Application.Filters.DateRange;

namespace ControlPeso.Web.Pages;

public partial class History
{
    [Inject] private IStringLocalizer<History> Localizer { get; set; } = null!;
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ILogger<History> Logger { get; set; } = null!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = null!;

    private MudDataGrid<WeightLogDto>? _grid;
    private bool _isLoading;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    private int _totalRecords;
    private decimal? _averageWeight;
    private decimal? _minWeight;
    private decimal? _maxWeight;

    // ========================================================================
    // LOCALIZED STRINGS
    // ========================================================================

    // Page & Meta
    private string PageTitle => Localizer["PageTitle"];
    private string MetaDescription => Localizer["MetaDescription"];
    private string MetaKeywords => Localizer["MetaKeywords"];
    private string OgTitle => Localizer["OgTitle"];
    private string OgDescription => Localizer["OgDescription"];

    // Page Header
    private string HistoryTitle => Localizer["HistoryTitle"];

    // Search & Filters
    private string SearchLabel => Localizer["SearchLabel"];
    private string DateFromLabel => Localizer["DateFromLabel"];
    private string DateToLabel => Localizer["DateToLabel"];
    private string ClearFiltersButton => Localizer["ClearFiltersButton"];

    // DataGrid Columns
    private string ColumnDate => Localizer["ColumnDate"];
    private string ColumnTime => Localizer["ColumnTime"];
    private string ColumnWeight => Localizer["ColumnWeight"];
    private string ColumnTrend => Localizer["ColumnTrend"];
    private string ColumnNote => Localizer["ColumnNote"];
    private string ColumnActions => Localizer["ColumnActions"];
    private string KgUnit => Localizer["KgUnit"];
    private string NoNote => Localizer["NoNote"];

    // Actions
    private string EditButtonAriaLabel => Localizer["EditButtonAriaLabel"];
    private string DeleteButtonAriaLabel => Localizer["DeleteButtonAriaLabel"];

    // DataGrid Messages
    private string NoRecordsFound => Localizer["NoRecordsFound"];

    // Stats Section
    private string StatsTitle => Localizer["StatsTitle"];
    private string TotalRecordsLabel => Localizer["TotalRecordsLabel"];
    private string AverageWeightLabel => Localizer["AverageWeightLabel"];
    private string MinWeightLabel => Localizer["MinWeightLabel"];
    private string MaxWeightLabel => Localizer["MaxWeightLabel"];
    private string NoDataDash => Localizer["NoDataDash"];

    // Dialogs
    private string ConfirmDeleteTitle => Localizer["ConfirmDeleteTitle"];
    private string DeleteDialogButton => Localizer["DeleteDialogButton"];

    // Messages
    private string ErrorLoadingRecords => Localizer["ErrorLoadingRecords"];
    private string RecordDeletedSuccess => Localizer["RecordDeletedSuccess"];
    private string ErrorDeletingRecord => Localizer["ErrorDeletingRecord"];
    private string EditFeatureComingSoon => Localizer["EditFeatureComingSoon"];

    // Methods with placeholders
    private string GetConfirmDeleteContent(string date, string time) => Localizer["ConfirmDeleteContent", date, time];

    private async Task<GridData<WeightLogDto>> LoadServerData(GridState<WeightLogDto> state, CancellationToken ct)
    {
        Logger.LogInformation("Loading weight logs - Page: {Page}, PageSize: {PageSize}", state.Page, state.PageSize);
        _isLoading = true;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Logger.LogError("User ID claim (NameIdentifier) not found or invalid format");
                return new GridData<WeightLogDto> { Items = [], TotalItems = 0 };
            }

            // Build filter with conditional DateRange
            WeightLogFilter filter;

            if (_dateFrom.HasValue || _dateTo.HasValue)
            {
                filter = new WeightLogFilter
                {
                    UserId = userId,
                    Page = state.Page + 1,
                    PageSize = state.PageSize,
                    DateRange = new AppDateRange
                    {
                        StartDate = _dateFrom.HasValue ? DateOnly.FromDateTime(_dateFrom.Value) : DateOnly.MinValue,
                        EndDate = _dateTo.HasValue ? DateOnly.FromDateTime(_dateTo.Value) : DateOnly.MaxValue
                    }
                };
            }
            else
            {
                filter = new WeightLogFilter
                {
                    UserId = userId,
                    Page = state.Page + 1,
                    PageSize = state.PageSize
                };
            }

            // Load data with cancellation token
            var result = await WeightLogService.GetByUserAsync(userId, filter, ct);

            // Update stats
            _totalRecords = result.TotalCount;
            if (result.Items.Count > 0)
            {
                _averageWeight = result.Items.Average(x => x.Weight);
                _minWeight = result.Items.Min(x => x.Weight);
                _maxWeight = result.Items.Max(x => x.Weight);
            }

            Logger.LogInformation("Loaded {Count} weight logs out of {Total}", result.Items.Count, result.TotalCount);

            return new GridData<WeightLogDto>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading weight logs");
            Snackbar.Add(ErrorLoadingRecords, Severity.Error);
            return new GridData<WeightLogDto> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OnSearchChanged(string value)
    {
        _searchText = value;
        Logger.LogDebug("Search text changed: {SearchText}", _searchText);

        if (_grid is not null)
        {
            await _grid.ReloadServerData();
        }
    }

    private async Task OnDateFromChanged(DateTime? value)
    {
        _dateFrom = value;
        Logger.LogDebug("Date from changed: {DateFrom}", _dateFrom);

        if (_grid is not null)
        {
            await _grid.ReloadServerData();
        }
    }

    private async Task OnDateToChanged(DateTime? value)
    {
        _dateTo = value;
        Logger.LogDebug("Date to changed: {DateTo}", _dateTo);

        if (_grid is not null)
        {
            await _grid.ReloadServerData();
        }
    }

    private async Task ClearFilters()
    {
        Logger.LogInformation("Clearing filters");

        _searchText = string.Empty;
        _dateFrom = null;
        _dateTo = null;

        if (_grid is not null)
        {
            await _grid.ReloadServerData();
        }
    }

    private async Task EditWeight(WeightLogDto weightLog)
    {
        Logger.LogInformation("Opening edit dialog for weight log {WeightLogId}", weightLog.Id);

        // TODO: Implementar EditWeightDialog cuando esté disponible
        Snackbar.Add(EditFeatureComingSoon, Severity.Info);
        await Task.CompletedTask;
    }

    private async Task DeleteWeight(WeightLogDto weightLog)
    {
        Logger.LogInformation("Requesting deletion confirmation for weight log {WeightLogId}", weightLog.Id);

        var parameters = new DialogParameters
        {
            ["ContentText"] = GetConfirmDeleteContent(weightLog.Date.ToString("dd/MM/yyyy"), weightLog.Time.ToString("HH:mm")),
            ["ButtonText"] = DeleteDialogButton,
            ["Color"] = Color.Error
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(ConfirmDeleteTitle, parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            try
            {
                Logger.LogInformation("Deleting weight log {WeightLogId}", weightLog.Id);

                await WeightLogService.DeleteAsync(weightLog.Id);

                Snackbar.Add(RecordDeletedSuccess, Severity.Success);

                if (_grid is not null)
                {
                    await _grid.ReloadServerData();
                }

                Logger.LogInformation("Weight log {WeightLogId} deleted successfully", weightLog.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting weight log {WeightLogId}", weightLog.Id);
                Snackbar.Add(ErrorDeletingRecord, Severity.Error);
            }
        }
    }

    private static (string icon, Color color) GetTrendIconAndColor(WeightTrend trend) => trend switch
    {
        WeightTrend.Down => (Icons.Material.Filled.TrendingDown, Color.Success),
        WeightTrend.Up => (Icons.Material.Filled.TrendingUp, Color.Warning),
        WeightTrend.Neutral => (Icons.Material.Filled.TrendingFlat, Color.Default),
        _ => (Icons.Material.Filled.TrendingFlat, Color.Default)
    };
}
