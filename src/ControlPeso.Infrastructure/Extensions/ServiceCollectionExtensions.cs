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

        // Registrar DbContextFactory con SQL Server (REFACTORED for Blazor Server concurrency)
        // Connection string desde appsettings.json: "ConnectionStrings:DefaultConnection"
        // SQL Server Express (localhost\SQLEXPRESS) en Development
        // SQL Server 2022 container en Docker/Production
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.json");

        // AddDbContextFactory en lugar de AddDbContext para Blazor Server
        // Esto permite crear múltiples instancias de DbContext de forma segura en circuitos concurrentes
        services.AddDbContextFactory<ControlPesoDbContext>(options =>
        {
            // Detect provider from connection string
            // SQLite: "Data Source=path.db"
            // SQL Server: "Server=..." or contains "SqlServer"
            var isSqlite = connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase)
                && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);

            if (isSqlite)
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                // Use SQL Server provider (SQL Server Express locally, SQL Server in Docker/Production)
                options.UseSqlServer(connectionString);
            }

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

        // Registrar IDbContextFactory<DbContext> genérico para Application layer
        // Application services inyectan IDbContextFactory<DbContext> sin conocer ControlPesoDbContext
        services.AddSingleton<IDbContextFactory<DbContext>>(provider =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();
            return new DbContextFactoryWrapper(factory);
        });

        // BACKWARD COMPATIBILITY: Registrar DbContext scoped para servicios que aún no han migrado
        // Este registration se puede eliminar una vez que TODOS los servicios usen IDbContextFactory
        services.AddScoped<DbContext>(provider =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();
            return factory.CreateDbContext();
        });

        // BACKWARD COMPATIBILITY: Registrar ControlPesoDbContext scoped para Infrastructure services
        services.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();
            return factory.CreateDbContext();
        });

        // ⚠️ DbSeeder REMOVED - App works ONLY with real OAuth users
        // No fake/demo data seeding

        // Registrar servicios de Infrastructure
        services.AddScoped<Application.Interfaces.IUserPreferencesService, Services.UserPreferencesService>();
        services.AddScoped<Application.Interfaces.IPhotoStorageService, Services.LocalPhotoStorageService>();
        services.AddScoped<Application.Interfaces.IImageProcessingService, Services.ImageProcessingService>();
        services.AddScoped<Application.Interfaces.IUserNotificationService, Services.UserNotificationService>();

        // TODO: Agregar repositorios si se implementan (opcional - los servicios de Application
        // pueden usar DbContext directamente para simplicidad en MVP)
        // services.AddScoped<IWeightLogRepository, WeightLogRepository>();

        return services;
    }
}

/// <summary>
/// Wrapper para IDbContextFactory que convierte de ControlPesoDbContext específico a DbContext genérico.
/// Necesario porque IDbContextFactory es invariante (no covariante).
/// </summary>
internal sealed class DbContextFactoryWrapper : IDbContextFactory<DbContext>
{
    private readonly IDbContextFactory<ControlPesoDbContext> _innerFactory;

    public DbContextFactoryWrapper(IDbContextFactory<ControlPesoDbContext> innerFactory)
    {
        ArgumentNullException.ThrowIfNull(innerFactory);
        _innerFactory = innerFactory;
    }

    public DbContext CreateDbContext()
    {
        return _innerFactory.CreateDbContext();
    }

    public async Task<DbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _innerFactory.CreateDbContextAsync(cancellationToken);
    }
}
