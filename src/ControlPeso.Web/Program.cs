using System.Threading.RateLimiting;
using ControlPeso.Application.Extensions;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Shared.Resources.Extensions;
using ControlPeso.Web.Components;
using ControlPeso.Web.Extensions;
using ControlPeso.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using MudBlazor.Services;
using Serilog;
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure ThisCloud.Framework.Loggings.Serilog
// This should write to logs/controlpeso-.ndjson
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");

// Register ThisCloud logging services
builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");

// Log immediately after registration to verify initialization
Log.Information("ThisCloud.Framework.Loggings initialized successfully");

// ============================================================================
// 2. LOCALIZATION CONFIGURATION (FASE 10)
// ============================================================================

// 2.1. Add Shared.Resources Localization services
// Uses custom SharedResourceStringLocalizerFactory to load .resx from Shared.Resources assembly
// This maintains IStringLocalizer<T> injection in components while centralizing resources
builder.Services.AddSharedResourcesLocalization();

// 2.2. Configure supported cultures
var supportedCultures = new[]
{
    new System.Globalization.CultureInfo("es-AR"),  // Español (Argentina) - DEFAULT
    new System.Globalization.CultureInfo("en-US")   // English (United States)
};

builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    // Cultura default si no se puede determinar la del usuario
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("es-AR");

    // Culturas soportadas para formateo de números, fechas, monedas
    options.SupportedCultures = supportedCultures;

    // Culturas soportadas para textos de UI (IStringLocalizer)
    options.SupportedUICultures = supportedCultures;

    // Request culture provider order (intenta en este orden):
    // 1. CookieRequestCultureProvider: Cookie persistida por LanguageSelector
    // 2. AcceptLanguageHeaderRequestCultureProvider: Browser default (Accept-Language header)
    // 3. DefaultRequestCulture: es-AR (fallback final)
    options.RequestCultureProviders = new Microsoft.AspNetCore.Localization.IRequestCultureProvider[]
    {
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider(),
        new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider()
    };

    // Nombre de la cookie (default: .AspNetCore.Culture)
    // Personalizamos para claridad en DevTools
    options.SetDefaultCulture("es-AR");
});

// ============================================================================

// 3. Configure Forwarded Headers for production (NPM Plus reverse proxy)
builder.Services.AddForwardedHeadersConfiguration(builder.Environment);

// 3.5. Configure Production Security Policies (HSTS, HTTPS redirection)
builder.Services.AddProductionHsts(builder.Environment);
builder.Services.AddProductionHttpsRedirection(builder.Environment);

// 3.7. Add Memory Cache (required by UserService caching layer - FASE 11)
// CRÍTICO: Must be registered BEFORE Infrastructure/Application services that depend on it
// UserService uses IMemoryCache to prevent N+1 query problem (40+ queries causing crash)
builder.Services.AddMemoryCache(options =>
{
    // Optional: Configure memory limits (commented out = unlimited)
    // options.SizeLimit = 1024; // Max number of cache entries
    // options.CompactionPercentage = 0.25; // Evict 25% when limit reached
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(1); // Scan for expired entries every 1 minute
});

// 4. Register Infrastructure services (DbContext, repositories)
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// 5. Register Application services (business logic, DTOs, validators)
builder.Services.AddApplicationServices();

// 5. Add Blazor services with SignalR configuration for debugging
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 5.0.5. Configure Blazor Server options for large payloads (image cropping)
builder.Services.AddServerSideBlazor(options =>
{
    // DEBUGGING: Enable detailed errors to see JavaScript exceptions in logs
    options.DetailedErrors = builder.Environment.IsDevelopment();

    // Increase timeouts to prevent premature disconnections during debugging
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);

    // CRÍTICO: Increase buffered batches for large DialogResult payloads
    // Default 10 is too small for Base64 images from MudDialog
    // Set to 50 to accommodate cropped images (typically 20-500KB as Base64)
    options.MaxBufferedUnacknowledgedRenderBatches = 50;
});

// 5.0.6. Configure Circuit Options for large message payloads
builder.Services.Configure<CircuitOptions>(options =>
{
    // CRÍTICO: Must also configure CircuitOptions (used by Blazor Server rendering)
    // This setting affects the SignalR circuit used for component communication
    options.MaxBufferedUnacknowledgedRenderBatches = 50;
});

// 5.1. Add Circuit Handler for global error monitoring in Blazor Server
builder.Services.AddScoped<CircuitHandler, ControlPeso.Web.Services.GlobalCircuitHandler>();

// 5.5. Add HttpContextAccessor (necesario para cookies en Blazor Server)
builder.Services.AddHttpContextAccessor();

// 6. Add MudBlazor services
builder.Services.AddMudServices();

// 6.3. Add Storage Services (unified interface for localStorage, sessionStorage, cookies)
// IMPORTANTE: LocalStorageService es el predeterminado (IStorageService → LocalStorageService)
// Para usar otro storage, inyectar explícitamente (ej: IEnumerable<IStorageService> y resolver por tipo)
builder.Services.AddScoped<ControlPeso.Web.Services.Storage.IStorageService, ControlPeso.Web.Services.Storage.LocalStorageService>();
builder.Services.AddScoped<ControlPeso.Web.Services.Storage.LocalStorageService>();
builder.Services.AddScoped<ControlPeso.Web.Services.Storage.SessionStorageService>();
builder.Services.AddScoped<ControlPeso.Web.Services.Storage.CookieStorageService>();

// 6.5. Add Theme Service (gestión de tema con persistencia en localStorage/DB)
builder.Services.AddScoped<ControlPeso.Web.Services.ThemeService>();

// 6.5.5. Add User Notification Service (wrapper para Snackbar con verificación de preferencias)
builder.Services.AddScoped<ControlPeso.Web.Services.NotificationService>();

// 6.6. Add Notification Services (Telegram)
builder.Services.AddHttpClient<ControlPeso.Web.Services.INotificationService, ControlPeso.Web.Services.TelegramNotificationService>();
builder.Services.Configure<ControlPeso.Web.Services.TelegramOptions>(
    builder.Configuration.GetSection(ControlPeso.Web.Services.TelegramOptions.ConfigSection));

// 6.7. Add User State Service (shared state across components)
builder.Services.AddScoped<ControlPeso.Web.Services.UserStateService>();

// 7. Add Authentication & Authorization (Google OAuth + LinkedIn OAuth)
builder.Services.AddOAuthAuthentication(builder.Configuration);

// 8. Add Claims Transformation (populate custom claims from DB after OAuth login)
builder.Services.AddScoped<IClaimsTransformation, ControlPeso.Web.Services.UserClaimsTransformation>();

// 9. Add Rate Limiting (protection against brute force attacks)
builder.Services.AddRateLimiter(options =>
{
    // Fixed window policy: 10 requests per minute per IP
    options.AddPolicy("fixed", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        }));

    // OAuth endpoints policy: 5 login attempts per minute per IP (stricter)
    options.AddPolicy("oauth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // No queueing for OAuth (reject immediately)
        }));

    // Global rejection status code
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ============================================================================
// CRÍTICO: UseForwardedHeaders DEBE ser el PRIMER middleware (producción)
// - ANTES de UseExceptionHandler, UseHsts, UseHttpsRedirection, UseAuthentication
// - Necesario para que OAuth genere URLs https:// correctamente detrás de NPM Plus
// ============================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();

    // DEBUG: Log headers received from NPM Plus proxy (temporary for OAuth troubleshooting)
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(
            "🔍 HEADERS DEBUG: Scheme={Scheme}, Host={Host}, Path={Path}, XForwardedProto={XForwardedProto}, XForwardedHost={XForwardedHost}, XForwardedFor={XForwardedFor}",
            context.Request.Scheme,
            context.Request.Host,
            context.Request.Path,
            context.Request.Headers["X-Forwarded-Proto"].ToString(),
            context.Request.Headers["X-Forwarded-Host"].ToString(),
            context.Request.Headers["X-Forwarded-For"].ToString()
        );
        await next();
    });
}

// 1. Global exception handler - catches unhandled exceptions and sends to Telegram
app.UseGlobalExceptionHandler();
app.Logger.LogInformation("=== APP BUILT SUCCESSFULLY ===");

// ⚠️ DATABASE SEEDING PERMANENTLY REMOVED
// The app works ONLY with real data from Google OAuth users
// No fake/demo data will ever be inserted

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// HTTPS Redirection - puede deshabilitarse con variable de ambiente (útil para Docker local sin SSL)
var disableHttpsRedirection = app.Configuration.GetValue<bool>("DISABLE_HTTPS_REDIRECTION");
if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}
else
{
    app.Logger.LogWarning("HTTPS Redirection DISABLED (DISABLE_HTTPS_REDIRECTION=true) - Only for Docker local testing!");
}

// 2. Static files - serve BEFORE security headers to avoid CSP blocking local uploads
app.UseStaticFiles();

// 3. Security headers - after static files, before authentication
app.UseSecurityHeaders();

// 4. Rate limiting - protect against brute force attacks
app.UseRateLimiter();

// 5. Request duration tracking - identify slow operations
app.UseRequestDurationTracking();

// ============================================================================
// 6. LOCALIZATION MIDDLEWARE (FASE 10)
// CRÍTICO: UseRequestLocalization DEBE ir:
// - DESPUÉS de UseStaticFiles (para no procesar archivos estáticos)
// - ANTES de UseAuthentication (para que claims tengan cultura correcta)
// - ANTES de MapRazorComponents (para que componentes tengan cultura correcta)
// ============================================================================
app.UseRequestLocalization(
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>>().Value
);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Register authentication endpoints (OAuth Challenge + Logout)
app.MapAuthenticationEndpoints();

// Health check endpoint for Docker/Kubernetes
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health")
.ExcludeFromDescription(); // No mostrar en Swagger si se agrega

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Capture application stopping event to ensure logs are flushed
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var shutdownLogger = app.Services.GetRequiredService<ILogger<Program>>();
    shutdownLogger.LogInformation("Application shutting down - flushing logs");

    // Force Serilog flush (if using Serilog.Log static)
    Serilog.Log.CloseAndFlush();
});

app.Run();
