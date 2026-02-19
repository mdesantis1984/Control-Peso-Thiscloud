# Architecture Documentation - Control Peso Thiscloud

## Table of Contents

- [Overview](#overview)
- [Architectural Pattern](#architectural-pattern)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Layer Dependencies](#layer-dependencies)
- [Design Patterns](#design-patterns)
- [Data Flow](#data-flow)
- [Key Architectural Decisions](#key-architectural-decisions)
- [Diagrams](#diagrams)

## Overview

Control Peso Thiscloud is a **Blazor Server** web application for body weight tracking and analysis. The architecture follows **Onion (Clean) Architecture** principles, emphasizing:

- **Separation of Concerns**: Each layer has a distinct responsibility
- **Dependency Inversion**: Inner layers don't depend on outer layers
- **Testability**: Business logic isolated from infrastructure
- **Maintainability**: Changes localized to specific layers

## Architectural Pattern

### Onion Architecture (Clean Architecture)

```
┌──────────────────────────────────────────┐
│           Presentation Layer             │
│         (Blazor Server - Web)            │
│   Pages, Components, Middleware          │
└────────────────┬─────────────────────────┘
                 │ Depends on Application
                 ↓
┌──────────────────────────────────────────┐
│          Application Layer               │
│     Services, DTOs, Interfaces           │
│    Validators, Filters, Mappers          │
└────────────────┬─────────────────────────┘
                 │ Depends on Domain
                 ↓
┌──────────────────────────────────────────┐
│       Infrastructure Layer               │
│   DbContext, Repositories, Auth          │
│   External Services, File I/O            │
└────────────────┬─────────────────────────┘
                 │ Depends on Application + Domain
                 ↓
┌──────────────────────────────────────────┐
│            Domain Layer                  │
│   Entities, Enums, Exceptions            │
│        (Zero Dependencies)               │
└──────────────────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Dependencies | Examples |
|-------|----------------|--------------|----------|
| **Domain** | Business entities, enums, core exceptions | **None** (zero external dependencies) | `User`, `WeightLog`, `UserRole`, `WeightUnit` |
| **Application** | Business logic, service interfaces, DTOs | Domain only | `WeightLogService`, `UserService`, `CreateWeightLogDto` |
| **Infrastructure** | Data access, external services, I/O | Domain + Application | `ControlPesoDbContext`, `UserRepository`, OAuth |
| **Web (Presentation)** | UI, routing, user interaction, middleware | Application + Infrastructure (DI only) | `Dashboard.razor`, `AddWeightDialog`, Middlewares |

## Technology Stack

### Core Framework

- **.NET 10** (`net10.0`): Target framework
- **C# 14**: Language version (inferred from TFM, not explicitly set)
- **Blazor Server**: Interactive server-side rendering
- **ASP.NET Core**: Web host, middleware pipeline

### UI Framework

- **MudBlazor 8.0.0**: Material Design component library (exclusive UI framework)
- **Google Fonts (Roboto)**: Typography

### Data Access

- **Entity Framework Core 9.0.1**: ORM (Database First mode)
- **SQLite**: Development/MVP database
- **Microsoft.EntityFrameworkCore.Sqlite**: SQLite provider

### Authentication

- **ASP.NET Core Identity**: Cookie-based authentication
- **Google OAuth 2.0**: Google sign-in
- **LinkedIn OAuth 2.0**: LinkedIn sign-in (Microsoft.AspNetCore.Authentication.OAuth)

### Validation

- **FluentValidation 11.11.0**: DTO validation
- **FluentValidation.DependencyInjectionExtensions**: DI integration

### Logging

- **ThisCloud.Framework.Loggings.Serilog 1.0.86**: Structured logging with:
  - Serilog sinks: Console + File (NDJSON format, rolling)
  - Automatic PII redaction
  - Correlation ID tracking

### CSV Export

- **CsvHelper 33.0.1**: CSV generation for Admin panel exports

### Analytics

- **Google Analytics 4**: Traffic analytics (gtag.js)
- **Cloudflare Analytics**: Privacy-first analytics (optional)

### Development Tools

- **Visual Studio 2022+**: Primary IDE
- **GitHub Copilot**: AI-assisted development (Claude Sonnet 4.5 agent mode)
- **Git**: Version control
- **GitHub Actions**: CI/CD (planned)

## Project Structure

```
ControlPeso.Thiscloud.sln
│
├── src/
│   ├── ControlPeso.Domain/                  ← Core business entities
│   │   ├── Entities/                        ← Scaffolded from database
│   │   │   ├── User.cs
│   │   │   ├── WeightLog.cs
│   │   │   ├── UserPreference.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/                           ← Manual (map to SQL INTEGERs)
│   │   │   ├── UserRole.cs
│   │   │   ├── UserStatus.cs
│   │   │   ├── UnitSystem.cs
│   │   │   ├── WeightUnit.cs
│   │   │   └── WeightTrend.cs
│   │   └── Exceptions/                      ← Domain exceptions
│   │       ├── DomainException.cs
│   │       ├── NotFoundException.cs
│   │       └── ValidationException.cs
│   │
│   ├── ControlPeso.Application/             ← Business logic
│   │   ├── DTOs/                            ← Data Transfer Objects
│   │   │   ├── WeightLogDto.cs
│   │   │   ├── CreateWeightLogDto.cs
│   │   │   ├── UpdateWeightLogDto.cs
│   │   │   ├── UserDto.cs
│   │   │   ├── UpdateUserProfileDto.cs
│   │   │   ├── TrendAnalysisDto.cs
│   │   │   ├── WeightProjectionDto.cs
│   │   │   ├── WeightStatsDto.cs
│   │   │   └── AdminDashboardDto.cs
│   │   ├── Interfaces/                      ← Service contracts
│   │   │   ├── IWeightLogService.cs
│   │   │   ├── IUserService.cs
│   │   │   ├── ITrendService.cs
│   │   │   └── IAdminService.cs
│   │   ├── Services/                        ← Service implementations
│   │   │   ├── WeightLogService.cs
│   │   │   ├── UserService.cs
│   │   │   ├── TrendService.cs
│   │   │   └── AdminService.cs
│   │   ├── Validators/                      ← FluentValidation
│   │   │   ├── CreateWeightLogValidator.cs
│   │   │   ├── UpdateWeightLogValidator.cs
│   │   │   └── UpdateUserProfileValidator.cs
│   │   ├── Filters/                         ← Query filters
│   │   │   ├── WeightLogFilter.cs
│   │   │   ├── UserFilter.cs
│   │   │   └── DateRange.cs
│   │   ├── Mapping/                         ← Entity ↔ DTO mappers
│   │   │   ├── WeightLogMapper.cs
│   │   │   └── UserMapper.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── ControlPeso.Infrastructure/          ← Data access & external
│   │   ├── Data/
│   │   │   ├── ControlPesoDbContext.cs      ← EF Core DbContext (scaffolded)
│   │   │   ├── IDbSeeder.cs                 ← Seeder interface
│   │   │   └── DbSeeder.cs                  ← Initial data seeding
│   │   ├── Repositories/                    ← (Optional) Repository pattern
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   └── ControlPeso.Web/                     ← Blazor Server presentation
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   ├── MainLayout.razor.cs
│       │   │   ├── NavMenu.razor
│       │   │   └── NavMenu.razor.cs
│       │   ├── Pages/                       ← Public pages
│       │   │   ├── Login.razor
│       │   │   ├── Login.razor.cs
│       │   │   ├── Home.razor
│       │   │   ├── Home.razor.cs
│       │   │   ├── Error.razor
│       │   │   ├── Error.razor.cs
│       │   │   └── Logout.razor.cs
│       │   ├── Shared/                      ← Reusable components
│       │   │   ├── AddWeightDialog.razor
│       │   │   ├── AddWeightDialog.razor.cs
│       │   │   ├── WeightChart.razor
│       │   │   ├── WeightChart.razor.cs
│       │   │   ├── StatsCard.razor
│       │   │   ├── StatsCard.razor.cs
│       │   │   ├── TrendCard.razor
│       │   │   ├── TrendCard.razor.cs
│       │   │   ├── NotificationBell.razor
│       │   │   └── NotificationBell.razor.cs
│       │   ├── App.razor                    ← Root component
│       │   ├── App.razor.cs
│       │   ├── Routes.razor                 ← Routing config
│       │   └── _Imports.razor               ← Global imports
│       ├── Pages/                           ← Protected pages
│       │   ├── Dashboard.razor
│       │   ├── Dashboard.razor.cs
│       │   ├── Profile.razor
│       │   ├── Profile.razor.cs
│       │   ├── History.razor
│       │   ├── History.razor.cs
│       │   ├── Trends.razor
│       │   ├── Trends.razor.cs
│       │   ├── Admin.razor
│       │   ├── Admin.razor.cs
│       │   └── _Imports.razor
│       ├── Middleware/                      ← Custom middlewares
│       │   ├── GlobalExceptionMiddleware.cs
│       │   └── SecurityHeadersMiddleware.cs
│       ├── Extensions/                      ← Extension methods
│       │   ├── AuthenticationExtensions.cs  ← OAuth config
│       │   └── EndpointExtensions.cs        ← API endpoints
│       ├── Theme/
│       │   └── ControlPesoTheme.cs          ← MudBlazor theme
│       ├── wwwroot/                         ← Static files
│       │   ├── css/
│       │   ├── js/
│       │   ├── images/
│       │   │   └── README_og-image.md
│       │   ├── robots.txt
│       │   ├── sitemap.xml
│       │   └── favicon.png
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs                       ← Entry point
│
├── tests/
│   ├── ControlPeso.Domain.Tests/
│   ├── ControlPeso.Application.Tests/
│   └── ControlPeso.Infrastructure.Tests/
│
├── docs/
│   ├── schema/
│   │   └── schema_v1.sql                    ← Database schema (source of truth)
│   ├── Plan_ControlPeso_Thiscloud_v1_0.md   ← Master plan
│   ├── ARCHITECTURE.md                      ← This document
│   ├── DATABASE.md
│   ├── SECURITY.md
│   ├── SEO.md
│   ├── DEPLOYMENT.md
│   ├── WCAG_AA_AUDIT.md
│   ├── CLOUDFLARE_ANALYTICS.md
│   └── THISCLOUD_FRAMEWORK_INTEGRATION.md
│
├── Directory.Build.props                    ← Global MSBuild props
├── Directory.Packages.props                 ← Central Package Management
├── .gitignore
└── README.md
```

## Layer Dependencies

### CRITICAL RULE: Dependency Direction

```
Web → Application → Domain
        ↑
Infrastructure → Application + Domain
```

**Dependency Rules**:
1. **Domain** depends on **NOTHING** (zero external packages)
2. **Application** depends **ONLY** on **Domain**
3. **Infrastructure** depends on **Domain** + **Application** (implements interfaces)
4. **Web** depends on **Application** + **Infrastructure** (only for DI registration in `Program.cs`)

### Forbidden Dependencies

❌ **Domain** MUST NOT reference:
- EF Core
- ASP.NET Core
- MudBlazor
- Any external package

❌ **Application** MUST NOT reference:
- EF Core
- ASP.NET Core
- MudBlazor
- Infrastructure

❌ **Web** components MUST NOT:
- Access `DbContext` directly
- Implement business logic in `.razor` or `.razor.cs` files
- Reference Infrastructure services (only inject Application interfaces)

### Allowed Dependencies

✅ **Application** defines interfaces → **Infrastructure** implements
✅ **Web** injects Application interfaces via DI
✅ **Program.cs** registers Infrastructure services (only place Web references Infrastructure)

## Design Patterns

### 1. Service Layer Pattern

**Location**: Application layer

**Description**: Business logic encapsulated in service classes implementing interfaces.

```csharp
// Application/Interfaces/IWeightLogService.cs
public interface IWeightLogService
{
    Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByUserAsync(Guid userId, WeightLogFilter filter, CancellationToken ct = default);
}

// Application/Services/WeightLogService.cs
internal sealed class WeightLogService : IWeightLogService
{
    private readonly ControlPesoDbContext _context;
    private readonly ILogger<WeightLogService> _logger;

    public WeightLogService(ControlPesoDbContext context, ILogger<WeightLogService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        _context = context;
        _logger = logger;
    }

    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating weight log for user {UserId}", dto.UserId);
        
        // Business logic here
        var entity = WeightLogMapper.ToEntity(dto);
        _context.WeightLogs.Add(entity);
        await _context.SaveChangesAsync(ct);
        
        return WeightLogMapper.ToDto(entity);
    }
}
```

### 2. DTO Pattern

**Location**: Application layer

**Description**: Data Transfer Objects decouple domain entities from API/UI contracts.

```csharp
// Application/DTOs/WeightLogDto.cs
public sealed record WeightLogDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly Time { get; init; }
    public decimal Weight { get; init; }     // Always kg internally
    public WeightUnit DisplayUnit { get; init; }
    public string? Note { get; init; }
    public WeightTrend Trend { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Benefits**:
- Entities never exposed to UI (security)
- Type conversions centralized (string → Guid, int → enum)
- API contracts stable (entity changes don't break UI)

### 3. Mapper Pattern

**Location**: Application/Mapping

**Description**: Converts between entities (Domain) and DTOs (Application).

```csharp
// Application/Mapping/WeightLogMapper.cs
internal static class WeightLogMapper
{
    public static WeightLogDto ToDto(WeightLog entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new WeightLogDto
        {
            Id = Guid.Parse(entity.Id),              // string → Guid
            UserId = Guid.Parse(entity.UserId),
            Date = DateOnly.Parse(entity.Date),      // string → DateOnly
            Time = TimeOnly.Parse(entity.Time),      // string → TimeOnly
            Weight = entity.Weight,                   // double (kg)
            DisplayUnit = (WeightUnit)entity.DisplayUnit, // int → enum
            Trend = (WeightTrend)entity.Trend,
            CreatedAt = DateTime.Parse(entity.CreatedAt)
        };
    }
}
```

### 4. Validator Pattern (FluentValidation)

**Location**: Application/Validators

**Description**: Centralized validation rules for DTOs.

```csharp
// Application/Validators/CreateWeightLogValidator.cs
public sealed class CreateWeightLogValidator : AbstractValidator<CreateWeightLogDto>
{
    public CreateWeightLogValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Time).NotEmpty();
        RuleFor(x => x.Weight)
            .InclusiveBetween(20, 500)
            .WithMessage("Weight must be between 20 and 500 kg");
    }
}
```

### 5. Middleware Pattern

**Location**: Web/Middleware

**Description**: ASP.NET Core middleware for cross-cutting concerns.

```csharp
// Web/Middleware/GlobalExceptionMiddleware.cs
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 6. Code-Behind Pattern (Blazor)

**Location**: Web/Components, Web/Pages

**Description**: Separation of markup (.razor) and logic (.razor.cs).

```razor
<!-- Dashboard.razor (markup only) -->
@page "/dashboard"
@attribute [Authorize]

<PageTitle>Dashboard - Control Peso Thiscloud</PageTitle>

@if (_isLoading)
{
    <MudProgressCircular />
}
else
{
    <MudGrid>
        <StatsCard Title="Current Weight" Value="@_currentWeight" />
    </MudGrid>
}
```

```csharp
// Dashboard.razor.cs (logic only)
namespace ControlPeso.Web.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private IWeightLogService WeightLogService { get; set; } = default!;
    
    private bool _isLoading = true;
    private string _currentWeight = "...";

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        var userId = GetCurrentUserId();
        var stats = await WeightLogService.GetStatsAsync(userId);
        _currentWeight = $"{stats.CurrentWeight:F1} kg";
        _isLoading = false;
    }
}
```

### 7. Extension Method Pattern

**Location**: */Extensions directories

**Description**: Organize service registration with fluent API.

```csharp
// Application/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWeightLogService, WeightLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddValidatorsFromAssemblyContaining<CreateWeightLogValidator>();
        return services;
    }
}

// Usage in Program.cs
builder.Services.AddApplicationServices();
```

## Data Flow

### 1. User Interaction → Service → Database (Create Weight Log)

```
┌────────────┐
│   User     │ Types weight + clicks "Save"
└─────┬──────┘
      │
      ↓ Component event handler
┌─────────────────────┐
│ AddWeightDialog     │ .razor.cs → OnSave()
│ (Web/Components)    │
└─────┬───────────────┘
      │ 1. Create CreateWeightLogDto
      │ 2. Call IWeightLogService.CreateAsync()
      ↓
┌─────────────────────┐
│ WeightLogService    │ Application layer
│ (Application)       │
└─────┬───────────────┘
      │ 1. Validate DTO (FluentValidation)
      │ 2. Map DTO → Entity (WeightLogMapper)
      │ 3. Call DbContext.WeightLogs.Add()
      │ 4. SaveChangesAsync()
      ↓
┌─────────────────────┐
│ ControlPesoDbContext│ Infrastructure layer
│ (Infrastructure)    │
└─────┬───────────────┘
      │ EF Core executes INSERT
      ↓
┌─────────────────────┐
│   SQLite DB         │ controlpeso.db
│   (Data)            │
└─────────────────────┘
```

### 2. Database → Service → UI (Load Dashboard)

```
┌─────────────────────┐
│ Dashboard.razor.cs  │ OnInitializedAsync()
│ (Web/Pages)         │
└─────┬───────────────┘
      │ 1. Get current UserId (AuthenticationState)
      │ 2. Call IWeightLogService.GetStatsAsync(userId)
      ↓
┌─────────────────────┐
│ WeightLogService    │ Application layer
│ (Application)       │
└─────┬───────────────┘
      │ 1. Query DbContext.WeightLogs
      │ 2. Calculate stats (avg, min, max, trend)
      │ 3. Map Entity → WeightStatsDto
      ↓
┌─────────────────────┐
│ ControlPesoDbContext│ Infrastructure layer
│ (Infrastructure)    │
└─────┬───────────────┘
      │ EF Core executes SELECT
      ↓
┌─────────────────────┐
│   SQLite DB         │ controlpeso.db
│   (Data)            │
└─────┬───────────────┘
      │ Data returned
      ↓
┌─────────────────────┐
│ Dashboard.razor.cs  │ Update component state
│ (Web/Pages)         │ StateHasChanged() → re-render
└─────────────────────┘
```

## Key Architectural Decisions

### 1. Database First (NOT Code First)

**Decision**: SQL schema (`docs/schema/schema_v1.sql`) is the source of truth.

**Rationale**:
- Schema defines ALL constraints (NOT NULL, CHECK, FK, defaults)
- No EF Core migrations (error-prone, merge conflicts)
- Scaffold entities from DB: `dotnet ef dbcontext scaffold`
- Entities are READ-ONLY (never manually modified)

**Workflow**:
```bash
# 1. Modify schema SQL
vim docs/schema/schema_v1.sql

# 2. Apply to database
sqlite3 controlpeso.db < schema_v1.sql

# 3. Re-scaffold entities
dotnet ef dbcontext scaffold "Data Source=controlpeso.db" \
  Microsoft.EntityFrameworkCore.Sqlite \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --force
```

### 2. MudBlazor Exclusive UI Framework

**Decision**: NO raw HTML when MudBlazor component exists.

**Rationale**:
- Consistent Material Design
- Built-in ARIA/accessibility
- Dark theme by default
- No Bootstrap/Tailwind conflicts

**Examples**:
```razor
❌ BAD: <input type="text" />
✅ GOOD: <MudTextField />

❌ BAD: <button>Click</button>
✅ GOOD: <MudButton>Click</MudButton>

❌ BAD: <table>
✅ GOOD: <MudDataGrid>
```

### 3. Code-Behind Pattern (NO @code blocks)

**Decision**: ALL C# logic in `.razor.cs` files, ZERO `@code { }` blocks in `.razor`.

**Rationale**:
- Separation of concerns (markup vs logic)
- IntelliSense support better in `.cs` files
- Unit testing easier (no Razor compilation)
- Large components don't bloat markup

**Example**:
```razor
<!-- AddWeightDialog.razor (markup only) -->
<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="Weight" Label="Weight" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="OnSave">Save</MudButton>
    </DialogActions>
</MudDialog>
```

```csharp
// AddWeightDialog.razor.cs (logic only)
namespace ControlPeso.Web.Components.Shared;

public partial class AddWeightDialog
{
    [Parameter] public double Weight { get; set; }
    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

    private void OnSave()
    {
        MudDialog.Close(DialogResult.Ok(Weight));
    }
}
```

### 4. Weight Storage: ALWAYS Kilograms

**Decision**: Store ALL weights in **kg** internally, convert to **lb** only for display.

**Rationale**:
- Consistent calculations (no conversion bugs)
- Trends/stats accurate
- User can switch units without data corruption

**Implementation**:
```csharp
// Display layer converts to user's preferred unit
public string GetDisplayWeight(double weightKg, WeightUnit displayUnit)
{
    return displayUnit == WeightUnit.Kg
        ? $"{weightKg:F1} kg"
        : $"{weightKg * 2.20462:F1} lb"; // Convert kg → lb
}
```

### 5. SQLite (Dev) → SQL Server (Production) Ready

**Decision**: Use SQLite for MVP, architecture supports easy swap to SQL Server.

**Rationale**:
- SQLite: zero-config, file-based, fast local development
- SQL Server: production-ready, scalable, enterprise features
- Swap only requires connection string + provider change

**Swap Process**:
```csharp
// appsettings.json
"ConnectionStrings": {
    // Development
    "DefaultConnection": "Data Source=controlpeso.db"
    
    // Production (SQL Server)
    // "DefaultConnection": "Server=...;Database=ControlPeso;..."
}

// Program.cs Infrastructure registration
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    if (environment.IsDevelopment())
    {
        services.AddDbContext<ControlPesoDbContext>(options =>
            options.UseSqlite(connectionString));
    }
    else
    {
        services.AddDbContext<ControlPesoDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}
```

### 6. Structured Logging with Redaction

**Decision**: ThisCloud.Framework.Loggings.Serilog with automatic PII redaction.

**Rationale**:
- Structured logging (JSON, queryable)
- Correlation ID tracking (distributed tracing ready)
- Automatic redaction of secrets (Authorization headers, tokens, passwords)
- File rolling (10MB chunks, 30 files retained)

**Configuration**:
```csharp
// Program.cs
builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "ControlPeso");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration);
```

### 7. Security Headers + CSP

**Decision**: Custom middleware for security headers with strict CSP.

**Rationale**:
- Protect against XSS, clickjacking, MIME sniffing
- CSP adapted to Blazor Server requirements (`unsafe-inline`, `unsafe-eval`)
- Permissions-Policy disables unused APIs (camera, microphone, geolocation)

**Headers Applied**:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy`: 13 directives (see `SecurityHeadersMiddleware.cs`)

### 8. Rate Limiting on OAuth Endpoints

**Decision**: Fixed window rate limiting (5 attempts/min per IP) on OAuth endpoints.

**Rationale**:
- Prevent brute force attacks on login
- Protect Google/LinkedIn OAuth quotas
- 429 Too Many Requests response after limit

**Configuration**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("oauth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1)
        }));
});

// Applied to endpoints
authGroup.MapGet("/login/google", ...).RequireRateLimiting("oauth");
```

## Diagrams

### High-Level Component Diagram

```
┌──────────────────────────────────────────────────────────┐
│                     Browser (Client)                     │
│  ┌────────┐  ┌──────────┐  ┌──────────┐  ┌───────────┐ │
│  │ Home   │  │ Login    │  │Dashboard │  │ Profile   │ │
│  │ Page   │  │ Page     │  │ Page     │  │ Page      │ │
│  └────────┘  └──────────┘  └──────────┘  └───────────┘ │
└─────────────────────┬────────────────────────────────────┘
                      │ SignalR WebSocket (Blazor Server)
                      ↓
┌──────────────────────────────────────────────────────────┐
│             ASP.NET Core Server (Web Layer)              │
│  ┌────────────┐  ┌─────────────┐  ┌──────────────────┐ │
│  │ Blazor     │  │ Middleware  │  │ Authentication   │ │
│  │ Components │  │ Pipeline    │  │ (OAuth)          │ │
│  └────────────┘  └─────────────┘  └──────────────────┘ │
└─────────────────────┬────────────────────────────────────┘
                      │ Dependency Injection
                      ↓
┌──────────────────────────────────────────────────────────┐
│            Application Layer (Services)                  │
│  ┌──────────────┐  ┌───────────┐  ┌─────────────────┐  │
│  │ WeightLog    │  │ User      │  │ Trend           │  │
│  │ Service      │  │ Service   │  │ Service         │  │
│  └──────────────┘  └───────────┘  └─────────────────┘  │
└─────────────────────┬────────────────────────────────────┘
                      │ EF Core DbContext
                      ↓
┌──────────────────────────────────────────────────────────┐
│         Infrastructure Layer (Data Access)               │
│  ┌───────────────────────────────────────────────────┐  │
│  │         ControlPesoDbContext (EF Core)            │  │
│  └───────────────────┬───────────────────────────────┘  │
└────────────────────────┬─────────────────────────────────┘
                         │ SQLite Provider
                         ↓
                  ┌──────────────┐
                  │ controlpeso  │
                  │    .db       │
                  └──────────────┘
```

### Authentication Flow (OAuth)

```
┌──────┐                                       ┌────────────┐
│User  │                                       │ Browser    │
└──┬───┘                                       └──────┬─────┘
   │ 1. Click "Login with Google"                    │
   │────────────────────────────────────────────────>│
   │                                                  │
   │ 2. GET /api/auth/login/google                   │
   │────────────────────────────────────────────────>│
   │                                       ┌──────────┴─────────┐
   │                                       │ EndpointExtensions │
   │                                       │ (Web/Extensions)   │
   │                                       └──────────┬─────────┘
   │ 3. Challenge(properties, ["Google"])            │
   │<────────────────────────────────────────────────│
   │                                                  │
   │ 4. Redirect to Google OAuth                     │
   │────────────────────────────────────────────────>│
   │                                       ┌──────────┴──────────┐
   │                                       │ accounts.google.com │
   │                                       └──────────┬──────────┘
   │ 5. User logs in to Google                       │
   │────────────────────────────────────────────────>│
   │                                                  │
   │ 6. Google redirects to callback (/signin-google)│
   │<────────────────────────────────────────────────│
   │                                       ┌──────────┴─────────┐
   │                                       │ Authentication     │
   │                                       │ Extensions         │
   │                                       │ (OnCreatingTicket) │
   │                                       └──────────┬─────────┘
   │ 7. Call IUserService.CreateOrUpdateFromOAuthAsync
   │────────────────────────────────────────────────>│
   │                                       ┌──────────┴─────────┐
   │                                       │ UserService        │
   │                                       │ (Application)      │
   │                                       └──────────┬─────────┘
   │ 8. Upsert User in DB                            │
   │────────────────────────────────────────────────>│
   │                                       ┌──────────┴─────────┐
   │                                       │ DbContext          │
   │                                       │ (Infrastructure)   │
   │                                       └────────────────────┘
   │                                                  │
   │ 9. Issue authentication cookie                  │
   │<────────────────────────────────────────────────│
   │                                                  │
   │ 10. Redirect to Dashboard                       │
   │<────────────────────────────────────────────────│
   │                                                  │
   │ 11. Navigate to /dashboard                      │
   │────────────────────────────────────────────────>│
   │                                       ┌──────────┴─────────┐
   │                                       │ Dashboard.razor    │
   │                                       │ [Authorize]        │
   │                                       └────────────────────┘
```

## Maintainability Checklist

- [ ] **Follow Onion dependencies**: Never violate layer dependencies
- [ ] **Database changes**: Always start with SQL → scaffold → (value converters if needed)
- [ ] **NO @code blocks**: All C# logic in `.razor.cs` files
- [ ] **Use MudBlazor**: Never use raw HTML when MudBlazor component exists
- [ ] **DTOs everywhere**: Never expose entities to Web layer
- [ ] **Validate inputs**: FluentValidation on all DTOs
- [ ] **Log structured**: Use `ILogger<T>` with named parameters
- [ ] **Async end-to-end**: Accept `CancellationToken`, propagate cancellation
- [ ] **Test coverage**: Unit tests for services, integration tests for DB access
- [ ] **Document decisions**: Update architecture docs when making significant changes

---

## 🆕 Claims Transformation Pattern (OAuth Integration)

### Overview

Control Peso implements the **Claims Transformation** pattern to enrich user identity with custom application-specific claims after OAuth authentication succeeds.

### Problem

Google/LinkedIn OAuth providers return standard claims (`sub`, `email`, `name`, `picture`), but the application needs:
- Internal GUID `UserId` (from database)
- `Role` (User/Administrator enum)
- `UserStatus` (Active/Inactive/Pending)
- `Language` preference (es/en)

**Challenge**: Claims added in `OnCreatingTicket` event don't persist to the authentication cookie.

### Solution: IClaimsTransformation

Implement `IClaimsTransformation` interface to add custom claims **after** cookie authentication middleware executes.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ 1. OAuth Callback (/signin-google)                         │
│    - Exchange authorization code for access token           │
│    - OnCreatingTicket: Create/update user in database       │
│    - Issue authentication cookie (standard claims only)     │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Cookie Authentication Middleware                         │
│    - Read encrypted cookie                                  │
│    - Validate cookie signature + expiration                 │
│    - Deserialize ClaimsPrincipal (standard OAuth claims)    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Claims Transformation Middleware                         │
│    - IClaimsTransformation.TransformAsync()                 │
│    - UserClaimsTransformation.cs (Web/Services)             │
│    - Check cache: if "UserId" claim exists, skip DB query   │
│    - Extract email from ClaimTypes.Email                    │
│    - Call IUserService.GetByEmailAsync(email)               │
│    - Add custom claims: UserId, Role, UserStatus, Language  │
│    - Return enriched ClaimsPrincipal                        │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Authorization Middleware                                 │
│    - Evaluate [Authorize] attributes with enriched claims   │
│    - [Authorize(Roles="Administrator")] checks Role claim   │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. Blazor Component Access                                  │
│    - @inject AuthenticationStateProvider AuthStateProvider  │
│    - var authState = await AuthStateProvider.GetAuthenticationStateAsync();
│    - var userId = authState.User.FindFirst("UserId")?.Value;│
│    - Custom claims available throughout application         │
└─────────────────────────────────────────────────────────────┘
```

### Implementation

**Step 1: Create Claims Transformation Service**

```csharp
// Web/Services/UserClaimsTransformation.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using ControlPeso.Application.Interfaces;

namespace ControlPeso.Web.Services;

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
        // 1. Validate authenticated
        if (!principal.Identity?.IsAuthenticated ?? false)
            return principal;

        // 2. Cache check: if UserId already exists, skip DB query
        if (principal.HasClaim(c => c.Type == "UserId"))
            return principal;

        // 3. Extract email from OAuth claims
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Claims transformation skipped - email claim not found");
            return principal;
        }

        // 4. Fetch user from database
        var user = await _userService.GetByEmailAsync(email!, CancellationToken.None);
        if (user is null)
        {
            _logger.LogWarning("Claims transformation skipped - user not found for email {Email}", email);
            return principal;
        }

        // 5. Create custom claims
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),              // Internal GUID
            new(ClaimTypes.Role, user.Role.ToString()),     // User/Administrator
            new("UserStatus", ((int)user.Status).ToString()), // Active/Inactive/Pending
            new("Language", user.Language)                   // es/en
        };

        // 6. Add claims to new identity and attach to principal
        var claimsIdentity = new ClaimsIdentity(claims);
        principal.AddIdentity(claimsIdentity);

        _logger.LogInformation(
            "Claims transformed successfully - UserId: {UserId}, Email: {Email}, Role: {Role}",
            user.Id, email, user.Role);

        return principal;
    }
}
```

**Step 2: Register in DI Container**

```csharp
// Program.cs (Web/Program.cs)
builder.Services.AddScoped<IClaimsTransformation, UserClaimsTransformation>();
```

**Step 3: Add GetByEmailAsync to UserService**

```csharp
// Application/Interfaces/IUserService.cs
public interface IUserService
{
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    // ... other methods
}

// Application/Services/UserService.cs
public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
{
    _logger.LogInformation("Fetching user by email: {Email}", email);

    var user = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email, ct);

    return user is null ? null : UserMapper.ToDto(user);
}
```

### Benefits

✅ **Persistent**: Custom claims persist in cookie, available on every request
✅ **Cached**: Cache check (`if (principal.HasClaim("UserId"))`) prevents repeated DB queries
✅ **Centralized**: All custom claims logic in one service
✅ **Separation**: OAuth logic (OnCreatingTicket) separate from app-specific claims
✅ **Flexible**: Easy to add more claims without touching OAuth config

### Execution Flow Log Example

```
[2026-02-19 15:30:45] [INF] OAuth callback received - sub: 108732892831748372819
[2026-02-19 15:30:45] [INF] User created/updated - Email: user@example.com, GoogleId: 108732...
[2026-02-19 15:30:45] [INF] Authentication cookie issued
[2026-02-19 15:30:45] [INF] Claims transformation executed - Email: user@example.com
[2026-02-19 15:30:45] [INF] Claims transformed successfully - UserId: 8a9b1c2d-..., Role: User
[2026-02-19 15:30:46] [INF] User profile loaded - UserId: 8a9b1c2d-..., Name: John Doe
```

---

## 🆕 Global Rendermode Configuration (Blazor Server)

### Problem

Blazor .NET 8+ requires **explicit rendermode** for interactive components (button clicks, form submissions, etc.).

**Initial Attempts**:
1. ❌ `@rendermode` on `RouteView` in `Routes.razor` → ERROR: "Serialization of System.Type not supported"
2. ❌ `@rendermode` on `MainLayout.razor` → ERROR: "Cannot pass Body parameter with rendermode" (RenderFragment not serializable)
3. ⚠️ `@rendermode` on each individual page → WORKS but repetitive (must add to Dashboard.razor, Profile.razor, History.razor, Trends.razor, Admin.razor, etc.)

### Solution: Global Rendermode in App.razor

**BEST PRACTICE**: Configure rendermode ONCE in `App.razor` on the `<Routes />` component.

### Implementation

```razor
<!-- Web/Components/App.razor -->
<!DOCTYPE html>
<html lang="es">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="ControlPeso.Web.styles.css" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>

<body>
    <!-- ✅ GLOBAL RENDERMODE: Applied to entire routing tree -->
    <Routes @rendermode="InteractiveServer" />

    <script src="_framework/blazor.web.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>

</html>
```

### Architecture Diagram

```
App.razor (Root Component)
└── <Routes @rendermode="InteractiveServer" />  ← SINGLE SOURCE OF TRUTH
    ├── Routes.razor
    │   └── <RouteView>  ← NO @rendermode (inherits from parent)
    │       └── MainLayout.razor  ← NO @rendermode (inherits)
    │           ├── NavMenu.razor  ← Inherits interactivity
    │           └── @Body (Page Content)
    │               ├── Dashboard.razor  ← Inherits interactivity
    │               │   ├── StatsCard.razor  ← Inherits interactivity
    │               │   ├── WeightChart.razor  ← Inherits interactivity
    │               │   └── AddWeightDialog.razor  ← Inherits interactivity
    │               ├── Profile.razor  ← Inherits interactivity
    │               ├── History.razor  ← Inherits interactivity
    │               ├── Trends.razor  ← Inherits interactivity
    │               └── Admin.razor  ← Inherits interactivity
    └── Login.razor  ← Inherits interactivity (no @layout, uses default)
```

### Benefits

✅ **DRY (Don't Repeat Yourself)**: One line configures entire app
✅ **No Serialization Errors**: Avoids System.Type and RenderFragment serialization issues
✅ **Maintainable**: Change rendermode strategy in one place
✅ **Inherited**: All routed pages and child components automatically interactive
✅ **Performance**: No overhead (same as individual page rendermodes)
✅ **Future-Proof**: Easy to switch to InteractiveWebAssembly or InteractiveAuto

### Alternative Rendermode Strategies

```razor
<!-- Server-side rendering (SSR) only - no interactivity -->
<Routes />  

<!-- Interactive server (SignalR WebSocket) - CURRENT CHOICE -->
<Routes @rendermode="InteractiveServer" />

<!-- Interactive WebAssembly (client-side .NET in browser) -->
<Routes @rendermode="InteractiveWebAssembly" />

<!-- Auto: Server for initial load, then WebAssembly when downloaded -->
<Routes @rendermode="InteractiveAuto" />
```

### Why InteractiveServer?

**Decision Rationale**:
- ✅ **Stateful**: User authentication state persists on server
- ✅ **Secure**: Business logic never exposed to client (no WASM download)
- ✅ **Fast Initial Load**: No .NET runtime download required
- ✅ **Database Access**: Direct DbContext access (no API layer needed)
- ⚠️ **Requires WebSocket**: Users must have persistent connection (SignalR)
- ⚠️ **Scalability**: Each user = active server connection (consider SignalR scale-out for production)

### Troubleshooting

**Problem**: Buttons not clickable, forms not submitting
**Solution**: Ensure `@rendermode="InteractiveServer"` on `<Routes />` in `App.razor`

**Problem**: "Serialization of System.Type not supported"
**Solution**: Remove `@rendermode` from `RouteView` in `Routes.razor`, apply to `<Routes />` in `App.razor`

**Problem**: "Cannot pass Body parameter with rendermode"
**Solution**: Remove `@rendermode` from `MainLayout.razor`, apply to `<Routes />` in `App.razor`

---

## References

- [Clean Architecture (Uncle Bob)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)
- [Blazor Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/blazor/)
- [Blazor Render Modes (.NET 8+)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [ASP.NET Core Claims Transformation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims)
- [MudBlazor Documentation](https://mudblazor.com/)
- [Entity Framework Core Best Practices](https://docs.microsoft.com/en-us/ef/core/)

