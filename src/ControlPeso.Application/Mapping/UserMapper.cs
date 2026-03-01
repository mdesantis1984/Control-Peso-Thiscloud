using ControlPeso.Application.DTOs;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.Mapping;

/// <summary>
/// Mapper estático para conversiones entre entidad User scaffolded y DTOs.
/// Responsable de conversiones de tipos (string→Guid, string→DateTime/DateOnly, double→decimal, int→enum).
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Convierte entidad scaffolded Users a UserDto.
    /// </summary>
    public static UserDto ToDto(Users entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new UserDto
        {
            Id = entity.Id,
            GoogleId = entity.GoogleId ?? string.Empty,
            Name = entity.Name,
            Email = entity.Email,
            Role = (UserRole)entity.Role,
            AvatarUrl = entity.AvatarUrl,
            MemberSince = entity.MemberSince,
            Height = entity.Height,
            UnitSystem = (UnitSystem)entity.UnitSystem,
            DateOfBirth = entity.DateOfBirth,
            Language = entity.Language,
            Status = (UserStatus)entity.Status,
            GoalWeight = entity.GoalWeight,
            StartingWeight = entity.StartingWeight,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Crea entidad Users nueva desde GoogleUserInfo (callback OAuth).
    /// Establece valores por defecto según schema SQL.
    /// </summary>
    public static Users ToEntity(GoogleUserInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var now = DateTime.UtcNow;

        return new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = info.GoogleId,
            Name = info.Name,
            Email = info.Email,
            Role = (int)UserRole.User,
            AvatarUrl = info.AvatarUrl,
            MemberSince = now,
            Height = 170.0m,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Actualiza entidad Users existente con datos de UpdateUserProfileDto.
    /// Solo actualiza campos editables por el usuario.
    /// NO modifica: Id, GoogleId, Email, Role, MemberSince, Status, CreatedAt.
    /// </summary>
    public static void UpdateEntity(Users entity, UpdateUserProfileDto dto)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(dto);

        entity.Name = dto.Name;
        entity.Height = dto.Height;
        entity.UnitSystem = (int)dto.UnitSystem;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Language = dto.Language;
        entity.GoalWeight = dto.GoalWeight;

        // Actualizar AvatarUrl si se proporciona
        if (dto.AvatarUrl is not null)
        {
            entity.AvatarUrl = dto.AvatarUrl;
        }

        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza campos sincronizables desde Google (nombre, avatar) si han cambiado.
    /// Se usa en el callback OAuth para mantener datos actualizados.
    /// IMPORTANTE: Preserva avatares personalizados (subidos localmente) y solo actualiza
    /// con el avatar de Google si el usuario no tiene un avatar personalizado.
    /// </summary>
    public static void UpdateFromGoogle(Users entity, GoogleUserInfo info)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(info);

        var changed = false;

        if (entity.Name != info.Name)
        {
            entity.Name = info.Name;
            changed = true;
        }

        if (entity.Email != info.Email)
        {
            entity.Email = info.Email;
            changed = true;
        }

        // Preservar avatares personalizados: solo actualizar si NO es un avatar local personalizado
        // Avatares personalizados empiezan con "/uploads/avatars/"
        // Avatares de Google empiezan con "https://" o son NULL
        var isCustomAvatar = entity.AvatarUrl?.StartsWith("/uploads/avatars/") == true;

        if (!isCustomAvatar && entity.AvatarUrl != info.AvatarUrl)
        {
            entity.AvatarUrl = info.AvatarUrl;
            changed = true;
        }

        if (changed)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Crea entidad Users nueva desde OAuthUserInfo genérico (Google o LinkedIn).
    /// Establece el campo GoogleId o LinkedInId según el provider.
    /// Nota: LinkedInId requiere P4.8 (columna DB) para funcionar.
    /// </summary>
    public static Users ToEntity(OAuthUserInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(info.Provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(info.ExternalId);

        var now = DateTime.UtcNow;

        var entity = new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = info.Provider == "Google" ? info.ExternalId : null,
            LinkedInId = info.Provider == "LinkedIn" ? info.ExternalId : null,
            Name = info.Name,
            Email = info.Email,
            Role = (int)UserRole.User,
            AvatarUrl = info.AvatarUrl,
            MemberSince = now,
            Height = 170.0m,
            UnitSystem = (int)UnitSystem.Metric,
            DateOfBirth = null,
            Language = "es",
            Status = (int)UserStatus.Active,
            GoalWeight = null,
            StartingWeight = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        return entity;
    }

    /// <summary>
    /// Actualiza campos sincronizables desde OAuth genérico (nombre, email, avatar) si han cambiado.
    /// Funciona con cualquier provider (Google, LinkedIn, etc.).
    /// IMPORTANTE: Preserva avatares personalizados (subidos localmente) y solo actualiza
    /// con el avatar del proveedor OAuth si el usuario no tiene un avatar personalizado.
    /// </summary>
    public static void UpdateFromOAuth(Users entity, OAuthUserInfo info)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(info);

        var changed = false;

        if (entity.Name != info.Name)
        {
            entity.Name = info.Name;
            changed = true;
        }

        if (entity.Email != info.Email)
        {
            entity.Email = info.Email;
            changed = true;
        }

        // Preservar avatares personalizados: solo actualizar si NO es un avatar local personalizado
        // Avatares personalizados empiezan con "/uploads/avatars/"
        // Avatares de OAuth empiezan con "https://" o son NULL
        var isCustomAvatar = entity.AvatarUrl?.StartsWith("/uploads/avatars/") == true;

        if (!isCustomAvatar && entity.AvatarUrl != info.AvatarUrl)
        {
            entity.AvatarUrl = info.AvatarUrl;
            changed = true;
        }

        if (changed)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
