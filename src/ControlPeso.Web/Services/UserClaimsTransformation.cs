using ControlPeso.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ControlPeso.Web.Services;

/// <summary>
/// Transforma claims del usuario después de autenticación OAuth.
/// Agrega claims personalizados desde la base de datos (UserId, Role, etc.)
/// </summary>
internal sealed class UserClaimsTransformation : IClaimsTransformation
{
    private readonly IUserService _userService;
    private readonly ILogger<UserClaimsTransformation> _logger;

    public UserClaimsTransformation(
        IUserService userService,
        ILogger<UserClaimsTransformation> logger)
    {
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(logger);

        _userService = userService;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Si el usuario no está autenticado, no hay nada que transformar
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return principal;
        }

        // Si ya transformamos este principal, skip (evitar queries redundantes y loops)
        if (principal.HasClaim(c => c.Type == "claims_transformed"))
        {
            return principal;
        }

        try
        {
            // Obtener email del claim OAuth (Google o LinkedIn)
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Email claim not found during claims transformation");
                return principal;
            }

            // Buscar usuario en DB por email
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found in DB during claims transformation - Email: {Email}", email);
                return principal;
            }

            // CRÍTICO: REMOVER claim NameIdentifier existente (GoogleId/LinkedInId del proveedor OAuth)
            var existingNameIdentifier = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (existingNameIdentifier != null)
            {
                identity.RemoveClaim(existingNameIdentifier);
                _logger.LogDebug("Removed existing NameIdentifier claim: {Value}", existingNameIdentifier.Value);
            }

            // Agregar claim NameIdentifier con UserId GUID del sistema (este es el que buscan las páginas)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

            // Agregar claims adicionales personalizados
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
            identity.AddClaim(new Claim("UserStatus", user.Status.ToString()));
            identity.AddClaim(new Claim("Language", user.Language));

            // Marcar como transformado para evitar re-transformaciones
            identity.AddClaim(new Claim("claims_transformed", "true"));

            _logger.LogInformation(
                "Claims transformed successfully - UserId: {UserId}, Email: {Email}, Role: {Role}",
                user.Id, user.Email, user.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming claims for authenticated user");
        }

        return principal;
    }
}
