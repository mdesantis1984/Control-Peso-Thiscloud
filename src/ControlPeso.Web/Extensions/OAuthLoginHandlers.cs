using System.Security.Claims;
using System.Text.Json;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace ControlPeso.Web.Extensions;

/// <summary>
/// Shared OAuth event handlers for Google and LinkedIn: map provider claims to a local user
/// (by provider id, never by an unverified email) and turn any failure into a login redirect.
/// </summary>
public static class OAuthLoginHandlers
{
    /// <summary>Query string value the login page understands for a failed external sign-in.</summary>
    public const string FailureQuery = "error=oauth_failed";

    /// <summary>
    /// Creates or links the local user for the authenticated provider identity.
    /// Failures (missing claims, rejected linking, database errors) propagate so the remote
    /// authentication handler reports them through <see cref="OnRemoteFailureAsync"/>.
    /// </summary>
    public static async Task OnCreatingTicketAsync(OAuthCreatingTicketContext context, string provider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var principal = context.Principal
            ?? throw new OAuthLoginException($"{provider} returned no identity.");

        var externalId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new OAuthLoginException($"{provider} 'sub' claim is missing.");
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new OAuthLoginException($"{provider} 'email' claim is missing.");
        }

        var name = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("name")?.Value;

        var oauthInfo = new OAuthUserInfo
        {
            Provider = provider,
            ExternalId = externalId,
            Name = string.IsNullOrWhiteSpace(name) ? email : name,
            Email = email,
            EmailVerified = IsEmailVerified(context.User),
            AvatarUrl = principal.FindFirst("picture")?.Value
        };

        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        var user = await userService.CreateOrUpdateFromOAuthAsync(oauthInfo, context.HttpContext.RequestAborted);

        // UserClaimsTransformation resolves the local user by email: pin it to the stored address so
        // an email claim asserted by the provider can never select a different account.
        if (principal.Identity is ClaimsIdentity identity)
        {
            foreach (var claim in identity.FindAll(c => c.Type == ClaimTypes.Email || c.Type == "email").ToList())
            {
                identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        }

        // NOTE: custom claims (UserId, Role) are added in UserClaimsTransformation.
    }

    /// <summary>
    /// Logs the failure and redirects to the login page instead of showing the error page.
    /// </summary>
    public static Task OnRemoteFailureAsync(RemoteFailureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<OAuthEvents>>();
        logger.LogError(context.Failure, "OAuth sign-in failed for scheme {Scheme}", context.Scheme.Name);

        context.Response.Redirect($"{context.Request.PathBase}/login?{FailureQuery}");
        context.HandleResponse();
        return Task.CompletedTask;
    }

    /// <summary>
    /// True only when the provider userinfo explicitly says <c>email_verified</c> is true
    /// (boolean <c>true</c> or the string "true"). Absent or any other value is unverified.
    /// </summary>
    private static bool IsEmailVerified(JsonElement user)
    {
        if (user.ValueKind != JsonValueKind.Object || !user.TryGetProperty("email_verified", out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
