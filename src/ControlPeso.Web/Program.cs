using ControlPeso.Application.Extensions;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Web.Components;
using ControlPeso.Web.Extensions;
using ControlPeso.Web.Middleware;
using MudBlazor.Services;
using System.Threading.RateLimiting;
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog FIRST (before any other service registration)
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");

// 2. Register ThisCloud logging services
builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");

// 3. Register Infrastructure services (DbContext, repositories)
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// 4. Register Application services (business logic, DTOs, validators)
builder.Services.AddApplicationServices();

// 5. Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 6. Add MudBlazor services
builder.Services.AddMudServices();

// 7. Add Authentication & Authorization (Google OAuth + LinkedIn OAuth)
builder.Services.AddOAuthAuthentication(builder.Configuration);

// 8. Add Rate Limiting (protection against brute force attacks)
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

// 1. Global exception handler - MUST BE FIRST middleware to catch all exceptions
app.UseGlobalExceptionHandler();

// Seed database in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ControlPeso.Infrastructure.Data.IDbSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 2. Security headers - after HTTPS redirection, before authentication
app.UseSecurityHeaders();

// 3. Rate limiting - protect against brute force attacks
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Register authentication endpoints (OAuth Challenge + Logout)
app.MapAuthenticationEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
