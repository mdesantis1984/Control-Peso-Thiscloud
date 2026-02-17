using ControlPeso.Application.DTOs;
using ControlPeso.Application.Filters;

namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    // GET operations
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetAllAsync(UserFilter filter, CancellationToken ct = default);

    // CUD operations
    Task<UserDto> CreateOrUpdateFromGoogleAsync(GoogleUserInfo info, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(Guid id, UpdateUserProfileDto dto, CancellationToken ct = default);
}
