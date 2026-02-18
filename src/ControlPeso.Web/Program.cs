using ControlPeso.Application.Extensions;
using ControlPeso.Infrastructure.Extensions;
using ControlPeso.Web.Components;
using ControlPeso.Web.Extensions;
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

// 7. Add Authentication & Authorization (Google OAuth + LinkedIn OAuth)
builder.Services.AddOAuthAuthentication(builder.Configuration);

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

// Register authentication endpoints (OAuth Challenge + Logout)
app.MapAuthenticationEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
