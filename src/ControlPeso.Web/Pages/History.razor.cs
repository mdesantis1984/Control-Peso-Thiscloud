using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;
using AppDateRange = ControlPeso.Application.Filters.DateRange;

namespace ControlPeso.Web.Pages;

public partial class History
{
    [Inject] private IWeightLogService WeightLogService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ILogger<History> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private MudDataGrid<WeightLogDto>? _grid;
    private bool _isLoading;

    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    private int _totalRecords;
    private decimal? _averageWeight;
    private decimal? _minWeight;
    private decimal? _maxWeight;

    private async Task<GridData<WeightLogDto>> LoadServerData(GridState<WeightLogDto> state)
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

            // Load data
            var result = await WeightLogService.GetByUserAsync(userId, filter);

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
            Snackbar.Add("Error al cargar los registros", Severity.Error);
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
        Snackbar.Add("Funcionalidad de edición próximamente", Severity.Info);
        await Task.CompletedTask;
    }

    private async Task DeleteWeight(WeightLogDto weightLog)
    {
        Logger.LogInformation("Requesting deletion confirmation for weight log {WeightLogId}", weightLog.Id);

        var parameters = new DialogParameters
        {
            ["ContentText"] = $"¿Estás seguro de eliminar el registro del {weightLog.Date:dd/MM/yyyy} a las {weightLog.Time:HH:mm}?",
            ["ButtonText"] = "Eliminar",
            ["Color"] = Color.Error
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirmar Eliminación", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            try
            {
                Logger.LogInformation("Deleting weight log {WeightLogId}", weightLog.Id);

                await WeightLogService.DeleteAsync(weightLog.Id);

                Snackbar.Add("Registro eliminado correctamente", Severity.Success);

                if (_grid is not null)
                {
                    await _grid.ReloadServerData();
                }

                Logger.LogInformation("Weight log {WeightLogId} deleted successfully", weightLog.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting weight log {WeightLogId}", weightLog.Id);
                Snackbar.Add("Error al eliminar el registro", Severity.Error);
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
