# Security Documentation - Control Peso Thiscloud

## Overview

Security-first design with OAuth 2.0, HTTPS enforcement, CSP headers, rate limiting, and structured logging with PII redaction.

## Authentication & Authorization

### OAuth 2.0 (Google + LinkedIn)

- **Cookie-based**: `HttpOnly`, `Secure`, `SameSite=Lax`
- **Expiration**: 30 days sliding
- **NO passwords stored**: External OAuth providers only

**OAuth Flow**:
1. User clicks "Login with Google/LinkedIn"
2. App redirects to OAuth provider
3. User authenticates (Google/LinkedIn handles credentials)
4. OAuth provider redirects to callback `/signin-google` or `/signin-linkedin`
5. `OnCreatingTicket` event: Create/update user in DB
6. Issue authentication cookie
7. Redirect to Dashboard

**Authorization**:
- `[Authorize]` attribute: Requires authenticated user
- `[Authorize(Roles="Administrator")]`: Admin-only pages

### Secrets Management

**Development**:
```bash
# User Secrets (NOT in Git)
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "GoogleAnalytics:MeasurementId" "G-XXXXXXXXX"
```

**Production**:
- Azure App Service: Application Settings (encrypted)
- IIS: Environment Variables
- **NEVER commit secrets to Git**

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
