using ControlPeso.Application.Extensions;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Web.Components;
using ControlPeso.Web.Extensions;
using ControlPeso.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using MudBlazor.Services;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
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


// 3. Register Infrastructure services (DbContext, repositories)
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// 4. Register Application services (business logic, DTOs, validators)
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

// 6.5. Add Theme Service (gestión de tema con persistencia en cookies)
builder.Services.AddScoped<ControlPeso.Web.Services.ThemeService>();

// 6.6. Add Notification Services (Telegram)
builder.Services.AddHttpClient<ControlPeso.Web.Services.INotificationService, ControlPeso.Web.Services.TelegramNotificationService>();
builder.Services.Configure<ControlPeso.Web.Services.TelegramOptions>(
    builder.Configuration.GetSection(ControlPeso.Web.Services.TelegramOptions.ConfigSection));

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

// 1. Global exception handler - catches unhandled exceptions and sends to Telegram
app.UseGlobalExceptionHandler();
app.Logger.LogInformation("=== APP BUILT SUCCESSFULLY ===");

// Seed database in Development environment
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== DATABASE SEEDING START ===");

        var seeder = scope.ServiceProvider.GetRequiredService<ControlPeso.Infrastructure.Data.IDbSeeder>();

        logger.LogInformation("DbSeeder instance created successfully");

        await seeder.SeedAsync();

        logger.LogInformation("=== DATABASE SEEDING COMPLETED ===");
    }
    catch (Exception ex)
    {
        // Log the error but DON'T crash the app
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "=== DATABASE SEEDING FAILED - App will continue without seed data ===");
        // Continue execution - the app should work even if seeding fails
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 2. Static files - serve BEFORE security headers to avoid CSP blocking local uploads
app.UseStaticFiles();

// 3. Security headers - after static files, before authentication
app.UseSecurityHeaders();

// 4. Rate limiting - protect against brute force attacks
app.UseRateLimiter();

// 5. Request duration tracking - identify slow operations
app.UseRequestDurationTracking();

app.UseAuthentication();

// DEVELOPMENT ONLY: Fake authentication middleware para bypass Google OAuth durante debugging con Chrome MCP
// Google detecta Chrome automatizado (MCP) como bot y bloquea login
// Este middleware simula usuario autenticado para permitir testing de páginas [Authorize]
app.UseDevelopmentAuth(app.Environment);

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
