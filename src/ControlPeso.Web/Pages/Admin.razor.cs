using System.Globalization;
using System.Text;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Pages;

public partial class Admin
{
    [Inject] private IAdminService AdminService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<Admin> Logger { get; set; } = default!;
    [Inject] private Services.NotificationService Snackbar { get; set; } = default!; // User notification service con verificación de preferencias

    private bool _isLoadingDashboard = true;
    private bool _isLoadingGrid;
    private bool _isExporting;
    private AdminDashboardDto? _dashboard;
    private MudDataGrid<UserDto>? _grid;

    // Filters
    private string? _searchTerm;
    private UserRole? _filterRole;
    private UserStatus? _filterStatus;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading admin dashboard");

        try
        {
            _dashboard = await AdminService.GetDashboardAsync();

            Logger.LogInformation(
                "Admin dashboard loaded - TotalUsers: {TotalUsers}, ActiveUsers: {ActiveUsers}, TotalWeightLogs: {TotalWeightLogs}",
                _dashboard.TotalUsers,
                _dashboard.ActiveUsers,
                _dashboard.TotalWeightLogs);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading admin dashboard");
            Snackbar.Add("Error al cargar el dashboard", Severity.Error);
        }
        finally
        {
            _isLoadingDashboard = false;
        }
    }

    private async Task<GridData<UserDto>> LoadServerData(GridState<UserDto> state, CancellationToken ct)
    {
        _isLoadingGrid = true;

        try
        {
            var filter = new UserFilter
            {
                SearchTerm = _searchTerm,
                Role = _filterRole,
                Status = _filterStatus,
                Page = state.Page + 1, // MudDataGrid is 0-based, filter is 1-based
                PageSize = state.PageSize,
                SortBy = state.SortDefinitions.FirstOrDefault()?.SortBy ?? "MemberSince",
                Descending = state.SortDefinitions.FirstOrDefault()?.Descending ?? true
            };

            Logger.LogInformation(
                "Loading users - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, Role: {Role}, Status: {Status}",
                filter.Page,
                filter.PageSize,
                filter.SearchTerm,
                filter.Role,
                filter.Status);

            var result = await AdminService.GetUsersAsync(filter, ct);

            Logger.LogInformation(
                "Users loaded - Count: {Count}, TotalCount: {TotalCount}",
                result.Items.Count,
                result.TotalCount);

            return new GridData<UserDto>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading users");
            Snackbar.Add("Error al cargar usuarios", Severity.Error);
            return new GridData<UserDto> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _isLoadingGrid = false;
        }
    }

    private void OnSearchChanged()
    {
        Logger.LogDebug("Search term changed: {SearchTerm}", _searchTerm);
        _grid?.ReloadServerData();
    }

    private void ClearFilters()
    {
        Logger.LogDebug("Clearing filters");
        _searchTerm = null;
        _filterRole = null;
        _filterStatus = null;
        _grid?.ReloadServerData();
    }

    private static Color GetRoleColor(UserRole role)
    {
        return role == UserRole.Administrator ? Color.Error : Color.Primary;
    }

    private static Color GetStatusColor(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => Color.Success,
            UserStatus.Inactive => Color.Error,
            UserStatus.Pending => Color.Warning,
            _ => Color.Default
        };
    }

    private static string GetStatusText(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "Activo",
            UserStatus.Inactive => "Inactivo",
            UserStatus.Pending => "Pendiente",
            _ => status.ToString()
        };
    }

    private async void OpenChangeRoleDialog(UserDto user)
    {
        Logger.LogInformation("Opening change role dialog for user {UserId}", user.Id);

        var parameters = new DialogParameters
        {
            ["UserName"] = user.Name,
            ["CurrentRole"] = user.Role
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ChangeRoleDialog>(
            "Cambiar Rol de Usuario",
            parameters,
            options);

        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is UserRole newRole)
        {
            await ChangeUserRole(user.Id, newRole);
        }
    }

    private async Task ChangeUserRole(Guid userId, UserRole newRole)
    {
        try
        {
            Logger.LogInformation(
                "Changing role for user {UserId} to {NewRole}",
                userId,
                newRole);

            await AdminService.UpdateUserRoleAsync(userId, newRole);

            Snackbar.Add("Rol actualizado correctamente", Severity.Success);

            // Refresh grid and dashboard
            await _grid!.ReloadServerData();
            _dashboard = await AdminService.GetDashboardAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing role for user {UserId}", userId);
            Snackbar.Add("Error al cambiar el rol", Severity.Error);
        }
    }

    private async void OpenChangeStatusDialog(UserDto user)
    {
        Logger.LogInformation("Opening change status dialog for user {UserId}", user.Id);

        var parameters = new DialogParameters
        {
            ["UserName"] = user.Name,
            ["CurrentStatus"] = user.Status
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ChangeStatusDialog>(
            "Cambiar Estado de Usuario",
            parameters,
            options);

        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is UserStatus newStatus)
        {
            await ChangeUserStatus(user.Id, newStatus);
        }
    }

    private async Task ChangeUserStatus(Guid userId, UserStatus newStatus)
    {
        try
        {
            Logger.LogInformation(
                "Changing status for user {UserId} to {NewStatus}",
                userId,
                newStatus);

            await AdminService.UpdateUserStatusAsync(userId, newStatus);

            Snackbar.Add("Estado actualizado correctamente", Severity.Success);

            // Refresh grid and dashboard
            await _grid!.ReloadServerData();
            _dashboard = await AdminService.GetDashboardAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing status for user {UserId}", userId);
            Snackbar.Add("Error al cambiar el estado", Severity.Error);
        }
    }

    private async Task ExportToCSV()
    {
        _isExporting = true;

        try
        {
            Logger.LogInformation("Exporting users to CSV");

            // Get ALL users (no pagination for export)
            var filter = new UserFilter
            {
                SearchTerm = _searchTerm,
                Role = _filterRole,
                Status = _filterStatus,
                Page = 1,
                PageSize = int.MaxValue // Get all matching users
            };

            var result = await AdminService.GetUsersAsync(filter);

            if (result.Items.Count == 0)
            {
                Snackbar.Add("No hay usuarios para exportar", Severity.Warning);
                return;
            }

            // Generate CSV
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true
            });

            // Map UserDto to CSV record
            var records = result.Items.Select(u => new UserCsvRecord
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role == UserRole.Administrator ? "Administrador" : "Usuario",
                Status = GetStatusText(u.Status),
                MemberSince = u.MemberSince.ToString("dd/MM/yyyy"),
                Height = $"{u.Height:F1}",
                UnitSystem = u.UnitSystem == UnitSystem.Metric ? "Métrico" : "Imperial",
                Language = u.Language,
                GoalWeight = u.GoalWeight.HasValue ? $"{u.GoalWeight.Value:F1}" : "",
                StartingWeight = u.StartingWeight.HasValue ? $"{u.StartingWeight.Value:F1}" : ""
            });

            csv.WriteRecords(records);
            await writer.FlushAsync();

            var csvData = Convert.ToBase64String(memoryStream.ToArray());
            var fileName = $"usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // Download file via JS Interop
            await JSRuntime.InvokeVoidAsync(
                "downloadFileFromBase64",
                fileName,
                "text/csv",
                csvData);

            Logger.LogInformation(
                "CSV export completed - {Count} users exported to {FileName}",
                result.Items.Count,
                fileName);

            Snackbar.Add($"{result.Items.Count} usuarios exportados correctamente", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting users to CSV");
            Snackbar.Add("Error al exportar usuarios", Severity.Error);
        }
        finally
        {
            _isExporting = false;
        }
    }

    // CSV Record class for export
    private sealed class UserCsvRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MemberSince { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string UnitSystem { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string GoalWeight { get; set; } = string.Empty;
        public string StartingWeight { get; set; } = string.Empty;
    }
}
