# Security Documentation - Control Peso Thiscloud

> **Last Updated**: 2026-02-19  
> **Status**: OAuth 2.0 Google Authentication ✅ IMPLEMENTED

## Overview

Security-first design with **Google OAuth 2.0**, HTTPS enforcement, CSP headers, rate limiting, and structured logging with PII redaction.

## Authentication & Authorization

### OAuth 2.0 Authentication

**Implemented**: ✅ Google OAuth 2.0 (LinkedIn UI removed, backend preserved)

#### Authentication Flow

1. **User Navigation**: User clicks "Continuar con Google" on `/login`
2. **OAuth Challenge**: App redirects to Google OAuth (`/api/auth/login/google`)
3. **User Consent**: User authenticates at Google (credentials handled by Google)
4. **OAuth Callback**: Google redirects to `/signin-google` with authorization code
5. **Token Exchange**: App exchanges code for access token (server-side)
6. **User Creation/Update**: `OnCreatingTicket` event extracts claims and creates/updates user in SQLite
7. **Claims Transformation**: `UserClaimsTransformation` service adds custom claims:
   - `UserId` (GUID from database)
   - `ClaimTypes.Role` (User/Administrator enum)
   - `UserStatus` (Active/Inactive/Pending)
   - `Language` (es/en for localization)
8. **Cookie Issuance**: Secure authentication cookie issued (`HttpOnly + Secure + SameSite=Lax`)
9. **Redirect**: User redirected to Dashboard

#### Cookie Configuration

```csharp
options.Cookie.Name = ".AspNetCore.Cookies";
options.Cookie.HttpOnly = true;   // JavaScript cannot access
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
options.Cookie.SameSite = SameSiteMode.Lax;  // CSRF protection
options.ExpireTimeSpan = TimeSpan.FromDays(30);  // 30-day expiration
options.SlidingExpiration = true;  // Renew on each request
```

#### Authorization Attributes

- **`[Authorize]`**: Requires authenticated user (any role)
- **`[Authorize(Roles="Administrator")]`**: Admin-only pages (Admin panel)
- **Anonymous**: Home page, Login page (no attribute)

#### Claims Transformation

Custom claims are added post-authentication via `IClaimsTransformation`:

```csharp
// UserClaimsTransformation.cs
public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
{
    if (!principal.Identity?.IsAuthenticated ?? false)
        return principal;

    // Check cache (if UserId already exists, skip DB query)
    if (principal.HasClaim(c => c.Type == "UserId"))
        return principal;

    // Fetch user from DB by email
    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
    var user = await _userService.GetByEmailAsync(email!);

    if (user is null)
        return principal;

    // Add custom claims
    var claims = new List<Claim>
    {
        new("UserId", user.Id.ToString()),
        new(ClaimTypes.Role, user.Role.ToString()),
        new("UserStatus", ((int)user.Status).ToString()),
        new("Language", user.Language)
    };

    var claimsIdentity = new ClaimsIdentity(claims);
    principal.AddIdentity(claimsIdentity);

    return principal;
}
```

**Benefits**:
- Custom claims available throughout app via `ClaimsPrincipal`
- Cached per request (no repeated DB queries)
- Clean separation: OAuth handles authentication, ClaimsTransformation handles app-specific claims

### Secrets Management

#### Development (Local)

**Docker**: Secrets in `docker-compose.override.yml` (gitignored)

```yaml
# docker-compose.override.yml (NOT in Git - in .gitignore)
version: '3.8'
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=YOUR_REAL_CLIENT_ID
      - Authentication__Google__ClientSecret=YOUR_REAL_CLIENT_SECRET
      - GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
```

**User Secrets** (Visual Studio / dotnet CLI):
```bash
# Alternative for non-Docker development
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "GoogleAnalytics:MeasurementId" "G-XXXXXXXXX"
```

**Storage Locations**:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`
- Docker: Environment variables via `docker-compose.override.yml`

#### Production

**Azure App Service**:
```bash
# Set via Azure Portal → Configuration → Application Settings
Authentication__Google__ClientId = "YOUR_PROD_CLIENT_ID"
Authentication__Google__ClientSecret = "YOUR_PROD_CLIENT_SECRET"
```

**Azure Key Vault** (recommended for production):
```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://controlpeso-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

**IIS / On-Premise**:
- Set environment variables in `web.config` or machine-level environment variables
- Use `<environmentVariables>` section in `web.config`

**NEVER**:
- ❌ Commit secrets to Git
- ❌ Hardcode secrets in `appsettings.json`
- ❌ Store secrets in plain text files
- ❌ Share secrets via email/Slack/Teams

### OAuth Provider Configuration

**Google Cloud Console** (https://console.cloud.google.com):

1. **Create Project**: `control-peso-thiscloud`
2. **Enable APIs**: Google+ API, Google Identity Toolkit API
3. **Create OAuth Client ID**:
   - Application type: Web application
   - Name: Control Peso Thiscloud (Production/Development)
   - Authorized redirect URIs:
     - Development: `http://localhost:8080/signin-google`
     - Production: `https://controlpeso.thiscloud.com.ar/signin-google`
4. **Obtain Credentials**: Download JSON with `client_id` and `client_secret`
5. **Configure Consent Screen**:
   - App name: Control Peso Thiscloud
   - User support email: support@thiscloud.com.ar
   - Developer contact: marco.alejandro.desantis@gmail.com
   - Scopes: `openid`, `profile`, `email`

**Scopes Requested**:
- `openid`: OpenID Connect authentication
- `profile`: Name, profile picture
- `email`: Email address (used as unique identifier)

## Security Headers (SecurityHeadersMiddleware)

### Applied Headers

| Header | Value | Purpose |
|--------|-------|---------|
| X-Content-Type-Options | nosniff | Prevent MIME type sniffing |
| X-Frame-Options | DENY | Prevent clickjacking (no iframes) |
| X-XSS-Protection | 0 | Disable legacy XSS filter (CSP replacement) |
| Referrer-Policy | strict-origin-when-cross-origin | Limit referrer leak |
| Permissions-Policy | camera=(), microphone=(), geolocation=(), payment=(), usb=() | Disable unused APIs |
| Content-Security-Policy | (see below) | Restrict resource origins |

### Content Security Policy (CSP)

```
default-src 'self';
script-src 'self' 'unsafe-inline' 'unsafe-eval'
           https://www.googletagmanager.com
           https://static.cloudflareinsights.com;
style-src 'self' 'unsafe-inline'
          https://fonts.googleapis.com;
font-src 'self' https://fonts.gstatic.com;
img-src 'self' data: https://*.googleusercontent.com https://*.licdn.com;
connect-src 'self' wss: https://www.google-analytics.com https://cloudflareinsights.com;
frame-ancestors 'none';
base-uri 'self';
form-action 'self';
upgrade-insecure-requests;
```

**Why `unsafe-inline` + `unsafe-eval`?**
- **Blazor Server requirement**: SignalR uses inline scripts + dynamic compilation
- **MudBlazor requirement**: Inline styles for components
- **Tradeoff**: Necessary for framework functionality, still protects against many XSS vectors

## Rate Limiting

### Policies

| Policy | Limit | Window | Endpoints |
|--------|-------|--------|-----------|
| `oauth` | 5 requests/IP | 1 minute | `/api/auth/login/google`, `/api/auth/login/linkedin` |
| `fixed` | 10 requests/IP | 1 minute | (Global, optional) |

**Response**: `429 Too Many Requests` when limit exceeded

**Implementation**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("oauth", context => 
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

authGroup.MapGet("/login/google", ...).RequireRateLimiting("oauth");
```

## HTTPS Enforcement

- **Middleware**: `UseHttpsRedirection()` (HTTP → HTTPS 301 redirect)
- **HSTS**: `UseHsts()` in Production (Strict-Transport-Security header)
- **Cookie**: `SecurePolicy.Always` (cookies only over HTTPS)

## Logging & Monitoring

### Structured Logging (Serilog + ThisCloud.Framework)

- **File**: `logs/controlpeso-YYYYMMDD.ndjson` (rolling 10MB, 30 files retained)
- **Console**: Colored output (Development)
- **Format**: NDJSON (newline-delimited JSON, queryable)

### PII Redaction (Automatic)

Redacts sensitive data in logs:
- `Authorization` headers
- `Cookie` headers
- JWT tokens
- Passwords (any field matching `password`, `pwd`, `secret`)

**Example**:
```csharp
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
// Output: { "UserId": "guid", "IpAddress": "[REDACTED]", ... }
```

### Correlation ID

- **Header**: `X-Correlation-Id` (auto-generated if missing)
- **Propagation**: Included in all logs for request tracing
- **Distributed tracing**: Ready for OpenTelemetry integration

## Exception Handling

### GlobalExceptionMiddleware

- **Catches**: ALL unhandled exceptions
- **Logs**: Structured with Path, Method, User, TraceId
- **Development**: Returns full stack trace
- **Production**: Returns generic message + TraceId (NO stack trace)

**Status Codes**:
- 400: `ArgumentException`, `ArgumentNullException`
- 401: `UnauthorizedAccessException`
- 409: `InvalidOperationException`
- 500: All other exceptions

## SQL Injection Prevention

- **EF Core parameterization**: All queries parameterized automatically
- **NO raw SQL**: Avoid `.FromSqlRaw()` unless absolutely necessary
- **Input validation**: FluentValidation on all DTOs

## XSS Prevention

1. **Blazor automatic escaping**: All `@variable` syntax escaped
2. **CSP headers**: Restrict inline scripts (with Blazor exceptions)
3. **Input validation**: Sanitize user inputs (names, notes, etc.)
4. **NO `@Html.Raw()`**: Never use unless content trusted (e.g., CMS)

## CSRF Prevention

- **Antiforgery tokens**: Enabled by default in Blazor Server
- **SameSite cookies**: `SameSite=Lax` prevents CSRF
- **`UseAntiforgery()` middleware**: Validates tokens on POST requests

## Sensitive Data Handling

### Passwords: ❌ NOT STORED
- OAuth-only authentication (Google/LinkedIn)
- NO local password storage

### Tokens: ⚠️ Handle Carefully
- OAuth tokens: `SaveTokens = true` (stored encrypted in cookie)
- **NEVER** log tokens or expose in API responses

### User Data: 🔒 Protected
- Weight logs: Private per user (query filtered by UserId)
- Email: Unique constraint, PII redacted in logs
- Avatar URL: Public (from OAuth provider)

## Vulnerability Scanning

### NuGet Packages
```bash
# Check for known vulnerabilities
dotnet list package --vulnerable --include-transitive

# Update packages
dotnet restore
```

### Dependency Updates
- Monitor GitHub Dependabot alerts
- Update packages regularly (Central Package Management in `Directory.Packages.props`)

## Security Checklist

- [x] OAuth 2.0 authentication (Google + LinkedIn)
- [x] HTTPS enforcement (`UseHttpsRedirection` + HSTS)
- [x] Secure cookies (`HttpOnly`, `Secure`, `SameSite`)
- [x] Security headers (CSP, X-Frame-Options, etc.)
- [x] Rate limiting on OAuth endpoints
- [x] Global exception handler (no info disclosure)
- [x] Structured logging with PII redaction
- [x] Input validation (FluentValidation)
- [x] SQL injection prevention (EF Core parameterization)
- [x] XSS prevention (Blazor escaping + CSP)
- [x] CSRF prevention (Antiforgery tokens)
- [ ] **TODO**: Implement security audit logging for Admin actions
- [ ] **TODO**: Add brute force detection (repeated failed logins)
- [ ] **TODO**: Implement account lockout after N failed attempts
- [ ] **TODO**: Add 2FA support (optional, post-MVP)

## Incident Response

### Data Breach
1. Isolate affected systems
2. Notify users within 72 hours (GDPR)
3. Review logs for unauthorized access
4. Rotate OAuth secrets
5. Force logout all users (clear cookies)

### Compromised Secrets
1. Rotate OAuth ClientId/ClientSecret immediately
2. Revoke Google/LinkedIn OAuth app access
3. Update User Secrets / Environment Variables
4. Redeploy application

### DDoS Attack
1. Enable Cloudflare DDoS protection
2. Increase rate limiting aggressiveness
3. Monitor logs for suspicious IPs
4. Consider IP blacklisting

## Compliance

### GDPR (General Data Protection Regulation)
- **Right to access**: Users can export data (Admin CSV export)
- **Right to deletion**: Implement user account deletion (TODO)
- **Data minimization**: Collect only necessary data
- **Consent**: OAuth login implies consent

### CCPA (California Consumer Privacy Act)
- **Do Not Sell**: No data sold to third parties
- **Opt-out**: Users can delete account (TODO)

### Security Standards
- **OWASP Top 10**: Addressed (injection, auth, XSS, etc.)
- **NIST Cybersecurity Framework**: Identify, Protect, Detect, Respond, Recover

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [CSP Reference](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [OAuth 2.0 Best Practices](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
