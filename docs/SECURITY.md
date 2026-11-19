# Security - Control Peso Thiscloud

Security model of the application as implemented. Operational details (secrets, rotation, deployment) are in [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md), [CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md) and [DEPLOYMENT.md](DEPLOYMENT.md).

## Summary

| Area | Control |
|------|---------|
| Authentication | Google OAuth 2.0 only; no local passwords |
| Session | Persistent cookie, `HttpOnly`, `Secure`, `SameSite=Lax`, 30 days sliding |
| Authorization | `[Authorize]` and role `Administrator` for `/admin` and `/diag-auth` |
| Transport | HTTPS redirection and HSTS (365 days, includes subdomains) in Production; TLS terminates at Cloudflare/NPMplus |
| Headers | CSP, `X-Frame-Options: DENY`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` |
| Abuse | Rate limiting on the OAuth login endpoints |
| Data | EF Core parameterized queries, FluentValidation, per-user data isolation |
| Container | Runs as non-root user `app` (UID 1654), verified by CI |
| Secrets | Environment only; gitleaks in CI |
| Logging | Structured NDJSON with redaction and correlation id |

## Authentication and authorization

### Flow

1. The user opens `/login` and chooses Google; the app calls `GET /api/auth/login/google` (rate limited).
2. The challenge redirects to Google. Credentials are handled only by Google.
3. Google redirects to `/signin-google` with an authorization code; the server exchanges it (scopes `profile` and `email`).
4. The user is created or updated in PostgreSQL (new users get role `User` and status `Active`) and the cookie is issued with `IsPersistent = true`.
5. `UserClaimsTransformation` adds application claims on each request: `UserId`, role, `UserStatus` and `Language`.
6. The post-login redirect only accepts local paths (`ToLocalUrl` rejects absolute URLs and forms such as `//host`).

### Cookie

```csharp
options.LoginPath = "/login";
options.LogoutPath = "/api/auth/logout";
options.AccessDeniedPath = "/access-denied";
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Lax;
options.ExpireTimeSpan = TimeSpan.FromDays(30);
options.SlidingExpiration = true;
```

Data Protection keys (which protect the cookie and antiforgery tokens) are persisted in `DataProtection:Path` (`/app/keys`, a Docker volume). If the volume is lost, all sessions are invalidated.

### Authorization

- Anonymous: home, `/login`, legal pages, `/health`, `/health/ready`, `/robots.txt`, `/sitemap.xml`.
- `[Authorize]`: dashboard, history, trends and profile.
- `[Authorize(Roles = "Administrator")]`: `/admin` and the diagnostics page `/diag-auth`.
- Accounts are created as `User`. The first administrator must be set in the database (`"Users"."Role" = 1`); afterwards administrators can manage roles from the admin panel.

## Security headers (`SecurityHeadersMiddleware`)

Applied to every non-static response (static paths such as `/css/`, `/js/`, `/images/`, `/uploads/` and common asset extensions are skipped).

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `X-XSS-Protection` | `0` (legacy filter disabled, CSP is used instead) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=(), usb=()` |
| `Content-Security-Policy` | see below |
| `Strict-Transport-Security` | Production only, via `UseHsts()` |

### Content Security Policy

```
default-src 'self';
script-src 'self' 'unsafe-inline' 'unsafe-eval' https://static.cloudflareinsights.com [+ https://www.googletagmanager.com];
style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
font-src 'self' https://fonts.gstatic.com;
img-src 'self' data: https://*.googleusercontent.com https://flagcdn.com [+ https://www.googletagmanager.com https://*.google-analytics.com];
connect-src 'self' wss: https://cloudflareinsights.com [+ https://www.googletagmanager.com https://*.google-analytics.com];
frame-ancestors 'none';
base-uri 'self';
form-action 'self';
upgrade-insecure-requests
```

The bracketed Google entries are added **only** when `GoogleAnalytics:MeasurementId` holds a real id (not empty, not a `YOUR_*` placeholder, only letters, digits and dashes).

`'unsafe-inline'` and `'unsafe-eval'` remain because Blazor Server and MudBlazor need them; this is a known tradeoff.

## Rate limiting

| Policy | Limit | Window | Applied to |
|--------|-------|--------|------------|
| `oauth` | 5 requests per IP | 1 minute | `/api/auth/login/google` |
| `fixed` | 10 requests per IP | 1 minute | Defined; use `RequireRateLimiting("fixed")` where needed |

Exceeding the limit returns `429 Too Many Requests`. `/health` is excluded. Behind a proxy the client IP comes from `X-Forwarded-For`, accepted only from the networks in `ForwardedHeaders:KnownNetworks` / `KnownProxies` (see [DEPLOYMENT.md](DEPLOYMENT.md)).

## HTTPS

- `UseHttpsRedirection()` and `UseHsts()` outside Development.
- `DISABLE_HTTPS_REDIRECTION=true` disables the redirect and logs a warning; use it only for local Docker without TLS.
- The authentication cookie is `Secure`, so login needs HTTPS (or a proxy that forwards `X-Forwarded-Proto: https`).

## Errors and logging

- `GlobalExceptionMiddleware` logs unhandled exceptions with path, method, user and `TraceIdentifier`, then redirects to `/error?traceId=...`. The page shows the trace id, never a stack trace. `DetailedErrors` for Blazor circuits are enabled only in Development.
- Errors are recorded **only in the logs**: structured NDJSON files in `/app/logs` (Serilog through `ThisCloud.Framework.Loggings`). There are no external alert channels.
- Redaction is enabled (`ThisCloud:Loggings:Redaction`) so sensitive fields such as authorization headers, cookies and tokens are masked.
- Every request has an `X-Correlation-Id` (generated if missing) included in the log entries.
- Never log tokens, secrets or full connection strings.

## Injection, XSS and CSRF

- **SQL**: Entity Framework Core parameterizes every query; avoid `FromSqlRaw`. Input is validated with FluentValidation in the Application layer.
- **XSS**: Blazor escapes `@value` output; avoid `MarkupString` with user content; CSP restricts origins.
- **CSRF**: `UseAntiforgery()` is enabled and the cookie is `SameSite=Lax`.
- **Uploads**: avatars are decoded and re-encoded server-side with SkiaSharp to WebP (the original file is never served) and stored under `wwwroot/uploads/avatars`.

## Data handling

- **Passwords**: none are stored.
- **OAuth tokens**: `SaveTokens = true` keeps them inside the encrypted cookie; they are never logged or returned by an API.
- **Weight data**: every query is filtered by the authenticated user's id; only administrators see user lists.
- **Email**: stored as `citext` with a unique index.
- **Analytics**: Google Analytics runs only when configured; Cloudflare Web Analytics is controlled in Cloudflare ([CLOUDFLARE_ANALYTICS.md](CLOUDFLARE_ANALYTICS.md)). Privacy policy: `/privacy`.

## Infrastructure

- Non-root container; only port 8080 is exposed and only to the reverse proxy.
- PostgreSQL runs on a separate host and must be reachable only from the Docker host.
- Daily database backups ([BACKUP_SYSTEM.md](BACKUP_SYSTEM.md)); backups contain personal data and must be protected.
- `.gitignore` excludes `.env*`, `appsettings.Development.json`, `appsettings.Production.json`, `docker-compose.override.yml` and `.secrets.local`.

## Continuous verification

`.github/workflows/ci.yml` runs on every pull request:

- build and tests;
- `dotnet list package --vulnerable --include-transitive` (fails on High or Critical findings);
- gitleaks on the pull request diff;
- Docker smoke test (image builds, runs as non-root, `/health` answers).

Locally:

```bash
dotnet list package --vulnerable --include-transitive
```

## Incident response

| Incident | Actions |
|----------|---------|
| Compromised secret | Rotate it immediately ([CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md)), review logs, redeploy |
| Suspected session theft | Reset the Data Protection keys (all users must sign in again) |
| Personal data breach | Isolate the host, review logs, rotate secrets and notify affected users as legally required |
| Abusive traffic | Block it in Cloudflare, review the logs for the offending IPs |

## Known limitations

- CSP needs `'unsafe-inline'` and `'unsafe-eval'` (Blazor Server, MudBlazor).
- Rate limiting is per instance and in memory (single container deployment).
- No two-factor authentication of its own: it depends on the Google account.

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
