using System.Text.Json;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Mapping;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Application.Services;

/// <summary>
/// Servicio para operaciones del panel de administración.
/// Gestiona usuarios, roles y genera métricas de sistema.
/// </summary>
public sealed class AdminService : IAdminService
{
    private readonly DbContext _context;
    private readonly ILogger<AdminService> _logger;
    private readonly IUserService _userService;

    public AdminService(
        DbContext context,
        ILogger<AdminService> logger,
        IUserService userService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userService);

        _context = context;
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Obtiene métricas del dashboard de administración.
    /// </summary>
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting admin dashboard metrics");

        try
        {
            var users = _context.Set<Users>().AsNoTracking();
            var weightLogs = _context.Set<WeightLogs>().AsNoTracking();

            // Contadores de usuarios por estado
            var totalUsers = await users.CountAsync(ct);
            var activeUsers = await users.CountAsync(u => u.Status == (int)UserStatus.Active, ct);
            var pendingUsers = await users.CountAsync(u => u.Status == (int)UserStatus.Pending, ct);
            var inactiveUsers = await users.CountAsync(u => u.Status == (int)UserStatus.Inactive, ct);

            // Contadores de weight logs
            var totalWeightLogs = await weightLogs.CountAsync(ct);

            var now = DateTime.UtcNow;
            var sevenDaysAgo = DateOnly.FromDateTime(now.AddDays(-7)).ToString("yyyy-MM-dd");
            var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30)).ToString("yyyy-MM-dd");
            var todayString = DateOnly.FromDateTime(now).ToString("yyyy-MM-dd");

            var weightLogsLastWeek = await weightLogs
                .CountAsync(wl => string.Compare(wl.Date, sevenDaysAgo) >= 0 &&
                                  string.Compare(wl.Date, todayString) <= 0, ct);

            var weightLogsLastMonth = await weightLogs
                .CountAsync(wl => string.Compare(wl.Date, thirtyDaysAgo) >= 0 &&
                                  string.Compare(wl.Date, todayString) <= 0, ct);

            // Usuario más reciente
            var latestUserCreatedAt = await users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => u.CreatedAt)
                .FirstOrDefaultAsync(ct);

            DateTime? latestUserRegistration = null;
            if (latestUserCreatedAt is not null)
            {
                latestUserRegistration = DateTime.Parse(latestUserCreatedAt);
            }

            var dashboard = new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                PendingUsers = pendingUsers,
                InactiveUsers = inactiveUsers,
                TotalWeightLogs = totalWeightLogs,
                WeightLogsLastWeek = weightLogsLastWeek,
                WeightLogsLastMonth = weightLogsLastMonth,
                LatestUserRegistration = latestUserRegistration
            };

            _logger.LogInformation(
                "Admin dashboard metrics retrieved - TotalUsers: {TotalUsers}, ActiveUsers: {ActiveUsers}, TotalWeightLogs: {TotalWeightLogs}",
                totalUsers, activeUsers, totalWeightLogs);

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin dashboard metrics");
            throw;
        }
    }

    /// <summary>
    /// Obtiene lista paginada de usuarios con filtros (delega a UserService).
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation("Admin getting users - Page: {Page}, PageSize: {PageSize}", filter.Page, filter.PageSize);

        try
        {
            // Delegar a UserService que ya tiene la lógica de paginación y filtros
            return await _userService.GetAllAsync(filter, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for admin panel - Page: {Page}", filter.Page);
            throw;
        }
    }

    /// <summary>
    /// Actualiza el rol de un usuario y registra la acción en AuditLog.
    /// </summary>
    public async Task UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating user role - UserId: {UserId}, NewRole: {NewRole}", userId, role);

        try
        {
            var user = await _context.Set<Users>()
                .FirstOrDefaultAsync(u => u.Id == userId.ToString(), ct);

            if (user is null)
            {
                _logger.LogWarning("User not found for role update: {UserId}", userId);
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            var oldRole = (UserRole)user.Role;
            
            if (oldRole == role)
            {
                _logger.LogInformation("User role already set to {Role}, no update needed: {UserId}", role, userId);
                return; // No hay cambio, no hacer nada
            }

            // Capturar snapshot del estado anterior
            var oldValue = new { Role = oldRole };
            var newValue = new { Role = role };

            // Actualizar rol
            user.Role = (int)role;
            user.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Crear entrada de auditoría
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(), // El usuario afectado
                Action = "UserRoleChanged",
                EntityType = "User",
                EntityId = userId.ToString(),
                OldValue = JsonSerializer.Serialize(oldValue),
                NewValue = JsonSerializer.Serialize(newValue),
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "User role updated successfully - UserId: {UserId}, OldRole: {OldRole}, NewRole: {NewRole}",
                userId, oldRole, role);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating user role - UserId: {UserId}, NewRole: {NewRole}",
                userId, role);
            throw;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw user not found exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user role - UserId: {UserId}, NewRole: {NewRole}",
                userId, role);
            throw;
        }
    }

    /// <summary>
    /// Actualiza el estado de un usuario y registra la acción en AuditLog.
    /// </summary>
    public async Task UpdateUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating user status - UserId: {UserId}, NewStatus: {NewStatus}", userId, status);

        try
        {
            var user = await _context.Set<Users>()
                .FirstOrDefaultAsync(u => u.Id == userId.ToString(), ct);

            if (user is null)
            {
                _logger.LogWarning("User not found for status update: {UserId}", userId);
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            var oldStatus = (UserStatus)user.Status;
            
            if (oldStatus == status)
            {
                _logger.LogInformation("User status already set to {Status}, no update needed: {UserId}", status, userId);
                return; // No hay cambio, no hacer nada
            }

            // Capturar snapshot del estado anterior
            var oldValue = new { Status = oldStatus };
            var newValue = new { Status = status };

            // Actualizar estado
            user.Status = (int)status;
            user.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Crear entrada de auditoría
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId.ToString(), // El usuario afectado
                Action = "UserStatusChanged",
                EntityType = "User",
                EntityId = userId.ToString(),
                OldValue = JsonSerializer.Serialize(oldValue),
                NewValue = JsonSerializer.Serialize(newValue),
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "User status updated successfully - UserId: {UserId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
                userId, oldStatus, status);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating user status - UserId: {UserId}, NewStatus: {NewStatus}",
                userId, status);
            throw;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw user not found exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user status - UserId: {UserId}, NewStatus: {NewStatus}",
                userId, status);
            throw;
        }
    }
}
