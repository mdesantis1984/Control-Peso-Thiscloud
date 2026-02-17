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

// 3. Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 4. Add MudBlazor services
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
