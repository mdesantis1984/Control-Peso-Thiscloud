using ControlPeso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Infrastructure.Extensions;

/// <summary>
/// Extension methods para registrar servicios de Infrastructure en DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos los servicios de Infrastructure (DbContext, repositorios, servicios externos).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration para obtener connection string.</param>
    /// <param name="environment">Environment para determinar configuración específica.</param>
    /// <returns>Service collection para chaining.</returns>
    /// <exception cref="ArgumentNullException">Si services o configuration son null.</exception>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Registrar DbContext con SQLite
        // Connection string desde appsettings.json: "ConnectionStrings:DefaultConnection"
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.json");

        services.AddDbContext<ControlPesoDbContext>(options =>
        {
            options.UseSqlite(connectionString);

            // En Development: habilitar logging detallado de queries SQL
            // En Production: solo errores
            var isDevelopment = environment?.IsDevelopment() ?? false;
            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(
                    message => System.Diagnostics.Debug.WriteLine(message),
                    new[] { DbLoggerCategory.Database.Command.Name },
                    LogLevel.Information);
            }
        });

        // Registrar DbContext genérico apuntando al específico
        // Esto permite que servicios de Application inyecten DbContext sin conocer ControlPesoDbContext
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ControlPesoDbContext>());

        // Registrar DbSeeder para seed de datos en Development
        services.AddScoped<IDbSeeder, DbSeeder>();

        // Registrar servicios de Infrastructure
        services.AddScoped<Application.Interfaces.IUserPreferencesService, Services.UserPreferencesService>();
        services.AddScoped<Application.Interfaces.IPhotoStorageService, Services.LocalPhotoStorageService>();
        services.AddScoped<Application.Interfaces.IImageProcessingService, Services.ImageProcessingService>();

        // TODO: Agregar repositorios si se implementan (opcional - los servicios de Application
        // pueden usar DbContext directamente para simplicidad en MVP)
        // services.AddScoped<IWeightLogRepository, WeightLogRepository>();

        return services;
    }
}
