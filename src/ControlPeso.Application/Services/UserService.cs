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
/// Servicio para gestión de usuarios (Users).
/// Implementa operaciones CRUD y sincronización con Google OAuth.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly DbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(
        DbContext context,
        ILogger<UserService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);

        try
        {
            var user = await _context.Set<Users>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id.ToString(), ct);

            if (user is null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return null;
            }

            var dto = UserMapper.ToDto(user);
            _logger.LogInformation("User retrieved successfully: {UserId}, Email: {Email}", id, dto.Email);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene un usuario por su Google ID.
    /// </summary>
    public async Task<UserDto?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId);

        _logger.LogInformation("Getting user by Google ID: {GoogleId}", googleId);

        try
        {
            var user = await _context.Set<Users>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

            if (user is null)
            {
                _logger.LogWarning("User not found with Google ID: {GoogleId}", googleId);
                return null;
            }

            var dto = UserMapper.ToDto(user);
            _logger.LogInformation("User retrieved by Google ID: {UserId}, Email: {Email}", dto.Id, dto.Email);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by Google ID: {GoogleId}", googleId);
            throw;
        }
    }

    /// <summary>
    /// Obtiene un usuario por su LinkedIn ID.
    /// Nota: Requiere P4.8 (agregar columna LinkedInId a Users table).
    /// </summary>
    public async Task<UserDto?> GetByLinkedInIdAsync(string linkedInId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkedInId);

        _logger.LogInformation("Getting user by LinkedIn ID: {LinkedInId}", linkedInId);

        try
        {
            // Nota: LinkedInId column no existe aún (P4.8 pending)
            // Esta query fallará hasta que se agregue la columna en schema SQL y re-scaffold
            var user = await _context.Set<Users>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => EF.Property<string>(u, "LinkedInId") == linkedInId, ct);

            if (user is null)
            {
                _logger.LogWarning("User not found with LinkedIn ID: {LinkedInId}", linkedInId);
                return null;
            }

            var dto = UserMapper.ToDto(user);
            _logger.LogInformation("User retrieved by LinkedIn ID: {UserId}, Email: {Email}", dto.Id, dto.Email);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by LinkedIn ID: {LinkedInId}", linkedInId);
            throw;
        }
    }

    /// <summary>
    /// Crea o actualiza un usuario desde información de Google OAuth.
    /// Si el usuario existe (por GoogleId), actualiza Name, Email, AvatarUrl.
    /// Si no existe, crea uno nuevo con status Active y role User.
    /// </summary>
    public async Task<UserDto> CreateOrUpdateFromGoogleAsync(GoogleUserInfo googleInfo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(googleInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(googleInfo.GoogleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(googleInfo.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(googleInfo.Name);

        _logger.LogInformation(
            "Creating or updating user from Google OAuth - GoogleId: {GoogleId}, Email: {Email}, Name: {Name}",
            googleInfo.GoogleId, googleInfo.Email, googleInfo.Name);

        try
        {
            var existingUser = await _context.Set<Users>()
                .FirstOrDefaultAsync(u => u.GoogleId == googleInfo.GoogleId, ct);

            if (existingUser is not null)
            {
                // Update existing user
                _logger.LogInformation("User exists, updating: {UserId}", existingUser.Id);

                UserMapper.UpdateFromGoogle(existingUser, googleInfo);

                await _context.SaveChangesAsync(ct);

                var updatedDto = UserMapper.ToDto(existingUser);
                _logger.LogInformation(
                    "User updated from Google OAuth: {UserId}, Email: {Email}, AvatarUrl: {HasAvatar}",
                    updatedDto.Id, updatedDto.Email, updatedDto.AvatarUrl is not null);

                return updatedDto;
            }

            // Create new user
            _logger.LogInformation("User does not exist, creating new user");

            var newUser = UserMapper.ToEntity(googleInfo);
            _context.Set<Users>().Add(newUser);
            await _context.SaveChangesAsync(ct);

            var createdDto = UserMapper.ToDto(newUser);
            _logger.LogInformation(
                "User created from Google OAuth: {UserId}, Email: {Email}, Role: {Role}, Status: {Status}",
                createdDto.Id, createdDto.Email, createdDto.Role, createdDto.Status);

            return createdDto;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Database error creating/updating user from Google OAuth - GoogleId: {GoogleId}, Email: {Email}",
                googleInfo.GoogleId, googleInfo.Email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error creating/updating user from Google OAuth - GoogleId: {GoogleId}, Email: {Email}",
                googleInfo.GoogleId, googleInfo.Email);
            throw;
        }
    }

    /// <summary>
    /// Crea o actualiza un usuario desde información de OAuth genérico (Google o LinkedIn).
    /// Query por provider-specific ID (GoogleId o LinkedInId).
    /// Si existe, actualiza Name, Email, AvatarUrl.
    /// Si no existe, crea uno nuevo con status Active y role User.
    /// Nota: LinkedIn requiere P4.8 (columna LinkedInId).
    /// </summary>
    public async Task<UserDto> CreateOrUpdateFromOAuthAsync(OAuthUserInfo oauthInfo, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(oauthInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauthInfo.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauthInfo.ExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauthInfo.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(oauthInfo.Name);

        _logger.LogInformation(
            "Creating or updating user from OAuth - Provider: {Provider}, ExternalId: {ExternalId}, Email: {Email}, Name: {Name}",
            oauthInfo.Provider, oauthInfo.ExternalId, oauthInfo.Email, oauthInfo.Name);

        try
        {
            // Query by provider-specific ID
            Users? existingUser = oauthInfo.Provider switch
            {
                "Google" => await _context.Set<Users>()
                    .FirstOrDefaultAsync(u => u.GoogleId == oauthInfo.ExternalId, ct),

                "LinkedIn" => await _context.Set<Users>()
                    .FirstOrDefaultAsync(u => EF.Property<string>(u, "LinkedInId") == oauthInfo.ExternalId, ct),

                _ => throw new NotSupportedException($"OAuth provider '{oauthInfo.Provider}' is not supported. Supported providers: Google, LinkedIn")
            };

            if (existingUser is not null)
            {
                // Update existing user
                _logger.LogInformation(
                    "User exists, updating: {UserId} - Provider: {Provider}",
                    existingUser.Id, oauthInfo.Provider);

                UserMapper.UpdateFromOAuth(existingUser, oauthInfo);

                await _context.SaveChangesAsync(ct);

                var updatedDto = UserMapper.ToDto(existingUser);
                _logger.LogInformation(
                    "User updated from {Provider} OAuth: {UserId}, Email: {Email}, AvatarUrl: {HasAvatar}",
                    oauthInfo.Provider, updatedDto.Id, updatedDto.Email, updatedDto.AvatarUrl is not null);

                return updatedDto;
            }

            // Create new user
            _logger.LogInformation(
                "User does not exist, creating new user - Provider: {Provider}",
                oauthInfo.Provider);

            var newUser = UserMapper.ToEntity(oauthInfo);
            _context.Set<Users>().Add(newUser);
            await _context.SaveChangesAsync(ct);

            var createdDto = UserMapper.ToDto(newUser);
            _logger.LogInformation(
                "User created from {Provider} OAuth: {UserId}, Email: {Email}, Role: {Role}, Status: {Status}",
                oauthInfo.Provider, createdDto.Id, createdDto.Email, createdDto.Role, createdDto.Status);

            return createdDto;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Database error creating/updating user from {Provider} OAuth - ExternalId: {ExternalId}, Email: {Email}",
                oauthInfo.Provider, oauthInfo.ExternalId, oauthInfo.Email);
            throw;
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex,
                "Unsupported OAuth provider: {Provider}",
                oauthInfo.Provider);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error creating/updating user from {Provider} OAuth - ExternalId: {ExternalId}, Email: {Email}",
                oauthInfo.Provider, oauthInfo.ExternalId, oauthInfo.Email);
            throw;
        }
    }

    /// <summary>
    /// Actualiza el perfil de un usuario.
    /// Solo actualiza campos no relacionados con autenticación (Name, Height, DateOfBirth, Language, GoalWeight, etc.).
    /// </summary>
    public async Task<UserDto> UpdateProfileAsync(Guid id, UpdateUserProfileDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        _logger.LogInformation(
            "Updating user profile: {UserId} - Name: {Name}, Height: {Height}, Language: {Language}",
            id, dto.Name, dto.Height, dto.Language);

        try
        {
            var user = await _context.Set<Users>()
                .FirstOrDefaultAsync(u => u.Id == id.ToString(), ct);

            if (user is null)
            {
                _logger.LogWarning("User not found for profile update: {UserId}", id);
                throw new InvalidOperationException($"User with ID {id} not found.");
            }

            UserMapper.UpdateEntity(user, dto);

            await _context.SaveChangesAsync(ct);

            var updatedDto = UserMapper.ToDto(user);
            _logger.LogInformation(
                "User profile updated successfully: {UserId}, Name: {Name}, Height: {Height}cm, GoalWeight: {GoalWeight}kg",
                id, updatedDto.Name, updatedDto.Height, updatedDto.GoalWeight);

            return updatedDto;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating user profile: {UserId}", id);
            throw;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw user not found exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user profile: {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene una lista paginada de usuarios (para panel de administración).
    /// Soporta filtrado por nombre, email, rol y estado.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetAllAsync(UserFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Getting paginated users - Page: {Page}, PageSize: {PageSize}, Search: {SearchTerm}, Role: {Role}, Status: {Status}",
            filter.Page, filter.PageSize, filter.SearchTerm, filter.Role, filter.Status);

        try
        {
            var query = _context.Set<Users>().AsNoTracking();

            // Apply search filter (name or email)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLowerInvariant();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            // Apply role filter
            if (filter.Role.HasValue)
            {
                query = query.Where(u => u.Role == (int)filter.Role.Value);
            }

            // Apply status filter
            if (filter.Status.HasValue)
            {
                query = query.Where(u => u.Status == (int)filter.Status.Value);
            }

            // Apply sorting (by name ascending by default)
            query = filter.Descending
                ? query.OrderByDescending(u => u.Name)
                : query.OrderBy(u => u.Name);

            // Get total count before pagination
            var totalItems = await query.CountAsync(ct);

            // Apply pagination
            var entities = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            var dtos = entities.Select(UserMapper.ToDto).ToList();

            var result = new PagedResult<UserDto>
            {
                Items = dtos,
                TotalCount = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            _logger.LogInformation(
                "Users retrieved successfully - Count: {Count}, TotalCount: {TotalCount}, Page: {Page}, TotalPages: {TotalPages}",
                dtos.Count, totalItems, filter.Page, result.TotalPages);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting paginated users - Page: {Page}, PageSize: {PageSize}",
                filter.Page, filter.PageSize);
            throw;
        }
    }
}
