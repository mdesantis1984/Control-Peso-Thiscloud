using ControlPeso.Application.Extensions;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Web.Components;
using MudBlazor.Services;
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

// 7. Add Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
})
.AddGoogle("Google", options =>
{
    var googleConfig = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleConfig["ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
    options.ClientSecret = googleConfig["ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");
    options.SaveTokens = true;

    // Scopes para obtener información del usuario
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
