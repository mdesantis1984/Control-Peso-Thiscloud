using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Services;
using ControlPeso.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ControlPeso.Application.Extensions;

/// <summary>
/// Extension methods para registrar servicios de la capa Application en el contenedor DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos los servicios de la capa Application.
    /// Incluye: servicios de dominio, validadores FluentValidation.
    /// </summary>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La misma colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registrar servicios de dominio (internos, solo accesibles vía interfaces)
        services.AddScoped<IWeightLogService, WeightLogService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITrendService, TrendService>();
        services.AddScoped<IAdminService, AdminService>();

        // Registrar validadores FluentValidation del assembly actual
        services.AddScoped<IValidator<CreateWeightLogDto>, CreateWeightLogValidator>();
        services.AddScoped<IValidator<UpdateWeightLogDto>, UpdateWeightLogValidator>();
        services.AddScoped<IValidator<UpdateUserProfileDto>, UpdateUserProfileValidator>();

        return services;
    }
}
