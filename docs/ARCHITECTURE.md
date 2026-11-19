# Architecture Documentation - Control Peso Thiscloud

> Verified against the code on 2026-11-19 (version 1.3.0, branch `feature/postgresql-migration`).

## Table of Contents

- [Overview](#overview)
- [Layers and Dependencies](#layers-and-dependencies)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Request Pipeline](#request-pipeline)
- [Design Patterns](#design-patterns)
- [Key Architectural Decisions](#key-architectural-decisions)
- [Authentication Flow](#authentication-flow)
- [Claims Transformation](#claims-transformation)
- [Render Mode](#render-mode)
- [Maintainability Checklist](#maintainability-checklist)
- [Related Documents](#related-documents)

## Overview

Control Peso Thiscloud is a **Blazor Server** web application for body weight tracking and analysis, built with **.NET 10** and **MudBlazor**. It is organized in layers following **Onion (Clean) Architecture**:

- **Separation of concerns**: each project has one responsibility.
- **Dependency inversion**: the Application layer defines interfaces, Infrastructure implements them.
- **Testability**: business logic is isolated from the web host and storage.

## Layers and Dependencies

```
Web ──────────────► Application ──► Domain
 │                      ▲              ▲
 ├──► Shared.Resources  │              │
 └──► Infrastructure ───┴──────────────┘
```

| Project | Responsibility | References |
|---------|----------------|------------|
| `ControlPeso.Domain` | Entities, enums, domain exceptions. No packages. | none |
| `ControlPeso.Application` | Services, service interfaces, DTOs, mappers, FluentValidation validators, filters | Domain |
| `ControlPeso.Infrastructure` | `ControlPesoDbContext` (EF Core + Npgsql), avatar image processing (SkiaSharp), photo storage, user notifications, user preferences | Domain, Application |
| `ControlPeso.Shared.Resources` | `.resx` localization resources (es-AR, en-US) and the localizer factory | none |
| `ControlPeso.Web` | Blazor Server UI, middleware, OAuth, endpoints, composition root (`Program.cs`) | Application, Infrastructure, Shared.Resources |

Rules that hold in the code today:

1. **Domain** has no package or project references.
2. **Application** references Domain only. It does reference `Microsoft.EntityFrameworkCore` (services use `IDbContextFactory<DbContext>` through an abstraction registered by Infrastructure), but never Infrastructure, ASP.NET Core or MudBlazor.
3. **Infrastructure** implements Application interfaces (`IUserPreferencesService`, `IPhotoStorageService`, `IImageProcessingService`, `IUserNotificationService`).
4. **Web** injects Application interfaces. It references Infrastructure only to register services in `Program.cs` (`AddInfrastructureServices`).

## Technology Stack

| Area | Technology |
|------|------------|
| Framework | .NET 10 (`net10.0`), C# latest, Blazor Server (`InteractiveServer`) |
| UI | MudBlazor 9.0.0 (Material Design) |
| Database | **PostgreSQL 15** through EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10 |
| Authentication | Cookie auth + Google OAuth 2.0 (login). LinkedIn OAuth is supported by the code but only registered when `Authentication:LinkedIn:ClientId` is configured |
| Validation | FluentValidation 12 |
| Logging | Serilog through `ThisCloud.Framework.Loggings.Serilog` (Console + rolling NDJSON file, PII redaction, correlation id) |
| Images | SkiaSharp 4.157 (MIT) + `SkiaSharp.NativeAssets.Linux.NoDependencies` |
| CSV export | CsvHelper 33 |
| Health checks | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` |
| Analytics | Google Analytics 4 only when `GoogleAnalytics:MeasurementId` is configured; Cloudflare Web Analytics beacon allowed by the CSP |
| Tests | xUnit, FluentAssertions, Moq/NSubstitute, bUnit; SQLite in-memory/InMemory providers for fast unit tests (test projects only), Testcontainers PostgreSQL for the real-database integration tests |
| CI | GitHub Actions (`.github/workflows/ci.yml`: build, tests, vulnerable-package check, gitleaks on the PR diff, Docker build + `/health` smoke test; `release.yml` tags and publishes releases on push to `main`) |

SQL Server and SQLite are **not** used at runtime. SQLite appears only in test projects.

## Project Structure

```
ControlPeso.slnx
├── src/
│   ├── ControlPeso.Domain/
│   │   ├── Entities/           Users, WeightLogs, UserPreferences, UserNotifications, AuditLog
│   │   ├── Enums/              UserRole, UserStatus, UnitSystem, WeightUnit, WeightTrend, NotificationSeverity
│   │   └── Exceptions/         DomainException, NotFoundException, ValidationException
│   ├── ControlPeso.Application/
│   │   ├── DTOs/  Filters/  Mapping/  Validators/  Logging/  Extensions/
│   │   ├── Interfaces/         IWeightLogService, IUserService, ITrendService, IAdminService,
│   │   │                       IUserNotificationService, IUserPreferencesService,
│   │   │                       IImageProcessingService, IPhotoStorageService
│   │   └── Services/           WeightLogService, UserService, TrendService, AdminService, AvatarHelper
│   ├── ControlPeso.Infrastructure/
│   │   ├── ControlPesoDbContext.cs, ControlPesoDbContextFactory.cs
│   │   ├── Migrations/         EF Core migrations (InitialPostgreSql)
│   │   ├── Data/               IDbSeeder, DbSeeder (not registered: the app never seeds data)
│   │   ├── Services/           ImageProcessingService, LocalPhotoStorageService,
│   │   │                       UserNotificationService, UserPreferencesService
│   │   └── Extensions/         ServiceCollectionExtensions (AddInfrastructureServices)
│   ├── ControlPeso.Shared.Resources/   .resx resources + Localization/ + Extensions/
│   └── ControlPeso.Web/
│       ├── Program.cs
│       ├── Components/         App.razor, Routes.razor, Layout/, Pages/ (Home, Login, Logout, Error), Shared/ (dialogs, charts, cards, notifications)
│       ├── Pages/              Dashboard, History, Trends, Profile, Admin, Legal/ (privacy, terms, changelog, licenses), Diagnostics/DiagAuth
│       ├── Middleware/         GlobalException, SecurityHeaders, RequestDuration, DevelopmentAuth
│       ├── Extensions/         Authentication, DataProtection, Endpoint, ForwardedHeaders, HealthEndpoints, SecurityPolicy, SeoEndpoints, OAuthLoginHandlers
│       ├── Services/           UserClaimsTransformation, UserStateService, ThemeService, NotificationService, SitemapService, GlobalCircuitHandler, Storage/
│       ├── Themes/             ControlPesoTheme (MudBlazor theme)
│       └── appsettings*.json   (+ .template/.example files)
├── tests/                      Domain, Application, Infrastructure (incl. Postgres/ Testcontainers), Shared.Resources, Web
├── docs/                       documentation, schema/postgresql_schema_v1.sql
├── scripts/                    migrate-postgres.sh (EF migration script generation/apply), deploy helpers
├── Dockerfile, docker-compose*.yml
├── Directory.Build.props       version, analyzers
└── Directory.Packages.props    Central Package Management
```

Routes: `/` (Home), `/login`, `/logout`, `/dashboard`, `/history`, `/trends`, `/profile`, `/admin`, `/privacy`, `/terms`, `/changelog`, `/licenses` (Spanish aliases `/privacidad`, `/terminos`, `/historial`, `/licencias`), `/diag-auth`, `/Error`. Minimal API endpoints: `/api/auth/login/google`, `/api/auth/login/linkedin`, `/api/auth/logout`, `/robots.txt`, `/sitemap.xml`, `/health`, `/health/ready`.

## Request Pipeline

Order in `Program.cs` (outermost first):

1. `UseForwardedHeaders` (non-Development) so OAuth builds `https://` URLs behind the Cloudflare/reverse proxy
2. `UseGlobalExceptionHandler` and `UseExceptionHandler("/Error")` + `UseHsts` (non-Development)
3. `UseHttpsRedirection` (can be disabled with `DISABLE_HTTPS_REDIRECTION=true` for local Docker)
4. `UseStaticFiles`
5. `UseSecurityHeaders` (CSP and friends, skipped for static assets)
6. `UseRateLimiter` (`fixed`: 10/min per IP, `oauth`: 5/min per IP)
7. `UseRequestDurationTracking` (slow request log, `Logging:SlowRequestThresholdMs`)
8. `UseRequestLocalization` (cookie, then `Accept-Language`; default `es-AR`, also `en-US`)
9. `UseAuthentication`, `UseAuthorization`, `UseAntiforgery`
10. `MapSeoEndpoints`, `MapAuthenticationEndpoints`, `MapApplicationHealthEndpoints`, `MapStaticAssets`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`

## Design Patterns

- **Service layer**: Application services behind interfaces, registered scoped in `AddApplicationServices()`.
- **Short-lived DbContexts**: services and Infrastructure classes obtain contexts from `IDbContextFactory<ControlPesoDbContext>` (or the `IDbContextFactory<DbContext>` wrapper exposed to Application). There is no scoped `DbContext` registration, because a scoped context would live as long as the Blazor circuit and is not thread-safe.
- **DTOs and mappers**: entities never reach the UI; `Mapping/` converts entities to DTOs (`WeightLogMapper`, `UserMapper`, `AuditLogMapper`).
- **Validators**: FluentValidation validators for `CreateWeightLogDto`, `UpdateWeightLogDto`, `UpdateUserProfileDto`.
- **Code-behind**: pages and components keep C# in `.razor.cs` partial classes. The only `@code` blocks that remain are tiny ones in `ConfirmDialog.razor`, `GoogleAnalytics.razor` and `Pages/Diagnostics/DiagAuth.razor`.
- **Extension methods** compose the host: `AddInfrastructureServices`, `AddApplicationServices`, `AddOAuthAuthentication`, `AddConfiguredDataProtection`, `AddForwardedHeadersConfiguration`, `AddApplicationHealthChecks`, `AddSharedResourcesLocalization`.
- **Caching**: `IMemoryCache` in `UserService` avoids repeated user lookups per circuit.

## Key Architectural Decisions

### PostgreSQL with EF Core migrations

- The runtime database is **PostgreSQL 15** (Npgsql). Entities live in `ControlPeso.Domain/Entities` and the model in `ControlPesoDbContext`. `docs/schema/postgresql_schema_v1.sql` is the reference schema.
- Schema changes go through EF Core migrations (`src/ControlPeso.Infrastructure/Migrations`, initial: `InitialPostgreSql`). The application **never migrates at startup**; the deploy step is scripted in `scripts/migrate-postgres.sh` (see [DATABASE.md](./DATABASE.md) and [DEPLOYMENT.md](./DEPLOYMENT.md)).
- PostgreSQL-specific mapping lives in `ControlPesoDbContext.ConfigureNpgsql`, applied only when the provider is Npgsql: `gen_random_uuid()` ids, `timestamp without time zone` columns, `citext` + unique index for `Users.Email`, unique index on `(UserId, Date, Time)` for weight logs, and `now() at time zone 'utc'` defaults.
- **UTC timestamps**: Npgsql refuses to write `DateTime` with `Kind=Utc` into `timestamp without time zone`. A value converter writes the value as `Unspecified` (same clock value) and reads it back as `Utc`. All timestamps in the application are UTC.
- The connection is configured with `EnableRetryOnFailure()`; manual transactions must run inside `CreateExecutionStrategy().ExecuteAsync`.

### Weight storage is always kilograms

`WeightLogs.Weight` is a `decimal` in kg. `DisplayUnit` (`WeightUnit.Kg`/`Lb`) and `Users.UnitSystem` (`Metric`/`Imperial`) only affect presentation; conversion happens in the UI layer.

### MudBlazor as the UI framework

Use MudBlazor components instead of raw HTML when an equivalent exists. The theme is defined in `Themes/ControlPesoTheme.cs` and dark mode is a per-user preference (`UserPreferences.DarkMode`).

### Structured logging with redaction

`ThisCloud.Framework.Loggings.Serilog` writes Console + NDJSON files (`logs/controlpeso-.ndjson`, 10 MB roll, 30 files retained) with PII redaction and `X-Correlation-Id`. Errors are recorded **only** in these logs (there is no external alert channel). See [LOGGING_BEST_PRACTICES.md](./LOGGING_BEST_PRACTICES.md), [LOGGING_QUERY_GUIDE.md](./LOGGING_QUERY_GUIDE.md) and [THISCLOUD_FRAMEWORK_INTEGRATION.md](./THISCLOUD_FRAMEWORK_INTEGRATION.md).

### Security headers and CSP

`SecurityHeadersMiddleware` adds `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy` and a Content-Security-Policy. `https://www.googletagmanager.com` and Google Analytics hosts are added to the CSP **only** when `GoogleAnalytics:MeasurementId` is configured. See [SECURITY.md](./SECURITY.md).

### Rate limiting

Two fixed-window policies (`fixed` 10 req/min and `oauth` 5 req/min per client IP) respond with HTTP 429.

### Persistent sign-in

The auth cookie is `HttpOnly`, `Secure`, `SameSite=Lax`, with a 30-day **sliding** expiration. The OAuth challenge sets `IsPersistent = true`, so the cookie survives browser restarts. Data Protection keys are persisted when `DataProtection:Path` is set (the Docker image uses `/app/keys`), so cookies also survive container restarts. `returnUrl` is accepted only as a local path on login and logout (open-redirect protection).

### Containers

The Docker image is based on `mcr.microsoft.com/dotnet/aspnet:10.0`, runs as the non-root user `app` (UID 1654) and exposes port 8080. `HEALTHCHECK` calls `/health`. `/health` is liveness (no database); `/health/ready` is readiness and returns 503 when PostgreSQL is unreachable.

## Authentication Flow

```
Browser ── GET /api/auth/login/google?returnUrl=/dashboard ──► Challenge (Google, IsPersistent)
Google  ── callback ──► OnCreatingTicket (OAuthLoginHandlers) ──► UserService.CreateOrUpdateFromOAuthAsync
        ── cookie issued (30 days, sliding) ──► redirect to local returnUrl
Next requests: cookie ──► UserClaimsTransformation adds NameIdentifier, Role, UserStatus, Language
Logout: GET /api/auth/logout?returnUrl=... ──► sign out ──► redirect to local path (default /login)
```

Failures at any point end in `OAuthLoginHandlers.OnRemoteFailureAsync`. Login with Google is the only sign-in method enabled in production. See [GOOGLE_OAUTH_SETUP.md](./GOOGLE_OAUTH_SETUP.md).

## Claims Transformation

`UserClaimsTransformation` (`IClaimsTransformation`, scoped) runs after authentication. It looks the user up by the email claim and adds claims from the database:

| Claim | Source |
|-------|--------|
| `ClaimTypes.NameIdentifier` | `Users.Id` |
| `ClaimTypes.Role` | `Users.Role` (`User` / `Administrator`) |
| `UserStatus` | `Users.Status` |
| `Language` | `Users.Language` |
| `claims_transformed` | marker so the principal is transformed only once |

This keeps the role and status authoritative in the database rather than in the cookie issued by the provider.

## Render Mode

`App.razor` applies `@rendermode="InteractiveServer"` globally to `HeadOutlet` and `Routes`, so every page runs as an interactive Blazor Server circuit. Consequences:

- Components can use `AuthenticationState`, `NavigationManager`, `ISnackbar` and JS interop without per-page render-mode attributes.
- Server-side prerendering still runs first: JS interop must not be called before `OnAfterRenderAsync(firstRender)`. See [technical/BLAZOR_PRERENDERING_JS_INTEROP.md](./technical/BLAZOR_PRERENDERING_JS_INTEROP.md).
- Circuit tuning lives in `Program.cs` (`DisconnectedCircuitRetentionPeriod` 3 min, `MaxBufferedUnacknowledgedRenderBatches` 50 for cropped avatar payloads).

## Maintainability Checklist

- [ ] Respect the layer dependencies above.
- [ ] Schema change = EF migration + updated `docs/schema/postgresql_schema_v1.sql`; never migrate at startup.
- [ ] No business logic in `.razor` files; C# lives in `.razor.cs`.
- [ ] Use MudBlazor components; expose DTOs, not entities, to the Web layer.
- [ ] Validate inputs with FluentValidation.
- [ ] Log with `ILogger<T>` and named parameters; never log secrets.
- [ ] Accept and propagate `CancellationToken`.
- [ ] Services get short-lived contexts from `IDbContextFactory`.
- [ ] Add tests with every behavior change (`dotnet test ControlPeso.slnx`; Postgres integration tests are skipped when Docker is unavailable).

## Related Documents

[DATABASE.md](./DATABASE.md) · [DEPLOYMENT.md](./DEPLOYMENT.md) · [SECURITY.md](./SECURITY.md) · [AVATAR_ARCHITECTURE.md](./AVATAR_ARCHITECTURE.md) · [NOTIFICATION_SYSTEM.md](./NOTIFICATION_SYSTEM.md) · [SEO_INFRASTRUCTURE.md](./SEO_INFRASTRUCTURE.md) · [documentacionfuncional.md](./documentacionfuncional.md)
