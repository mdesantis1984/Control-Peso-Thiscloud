using ControlPeso.Application.DTOs;
using ControlPeso.Application.Extensions;
using ControlPeso.Application.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ControlPeso.Application.Tests.Extensions;

/// <summary>
/// Tests for Application ServiceCollectionExtensions
/// Basic tests to verify DI registration
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationServices_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        // Act
        var act = () => ControlPeso.Application.Extensions.ServiceCollectionExtensions.AddApplicationServices(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddApplicationServices_ShouldReturnServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplicationServices();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterWeightLogService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IWeightLogService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterUserService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IUserService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterTrendService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITrendService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAdminService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAdminService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterCreateWeightLogValidator_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<CreateWeightLogDto>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterUpdateWeightLogValidator_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<UpdateWeightLogDto>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterUpdateUserProfileValidator_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<UpdateUserProfileDto>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAllServicesAsScoped_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var serviceTypes = new[]
        {
            typeof(IWeightLogService),
            typeof(IUserService),
            typeof(ITrendService),
            typeof(IAdminService)
        };

        foreach (var serviceType in serviceTypes)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
            descriptor.Should().NotBeNull($"{serviceType.Name} should be registered");
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAllValidatorsAsScoped_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationServices();

        // Assert
        var validatorTypes = new[]
        {
            typeof(IValidator<CreateWeightLogDto>),
            typeof(IValidator<UpdateWeightLogDto>),
            typeof(IValidator<UpdateUserProfileDto>)
        };

        foreach (var validatorType in validatorTypes)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == validatorType);
            descriptor.Should().NotBeNull($"{validatorType.Name} should be registered");
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }
    }
}
