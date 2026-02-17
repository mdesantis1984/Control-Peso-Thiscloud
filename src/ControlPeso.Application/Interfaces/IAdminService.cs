using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;
using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service interface for admin panel operations
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Get dashboard statistics for admin panel
    /// </summary>
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Update user role (creates audit log entry)
    /// </summary>
    Task UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);

    /// <summary>
    /// Update user status (creates audit log entry)
    /// </summary>
    Task UpdateUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct = default);
}
