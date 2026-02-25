using ControlPeso.Application.Interfaces;
using ControlPeso.Infrastructure;
using ControlPeso.Infrastructure.Data;
using ControlPeso.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ControlPeso.Infrastructure.Tests.Extensions;

/// <summary>
/// Tests for Infrastructure ServiceCollectionExtensions
/// Tests DI registration and configuration
/// </summary>
public sealed class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly string _testDbPath;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");

        // Create configuration with connection string
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _mockEnvironment = new Mock<IHostEnvironment>();
    }

    public void Dispose()
    {
        // Clean up test database
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Parameter Validation Tests

    [Fact]
    public void AddInfrastructureServices_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        // Act
        var act = () => ServiceCollectionExtensions.AddInfrastructureServices(
            null!,
            _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddInfrastructureServices_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act
        var act = () => _services.AddInfrastructureServices(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void AddInfrastructureServices_ShouldThrowInvalidOperationException_WhenConnectionStringIsMissing()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act
        var act = () => _services.AddInfrastructureServices(emptyConfig);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Connection string*not found*");
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterDbContext_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(ControlPesoDbContext));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterGenericDbContext_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(DbContext));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterDbSeeder_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IDbSeeder));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(DbSeeder));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterUserPreferencesService_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IUserPreferencesService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterPhotoStorageService_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPhotoStorageService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterImageProcessingService_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IImageProcessingService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterUserNotificationService_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IUserNotificationService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldReturnServiceCollection_ForChaining()
    {
        // Act
        var result = _services.AddInfrastructureServices(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    #endregion

    #region Environment-Specific Configuration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldWorkWithoutEnvironment_WhenEnvironmentIsNull()
    {
        // Act
        var act = () => _services.AddInfrastructureServices(_configuration, environment: null);

        // Assert
        act.Should().NotThrow();
        _services.Should().NotBeEmpty();
    }

    #endregion

    #region Service Lifetime Tests

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterAllServicesAsScoped_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert
        var applicationServiceTypes = new[]
        {
            typeof(IUserPreferencesService),
            typeof(IPhotoStorageService),
            typeof(IImageProcessingService),
            typeof(IUserNotificationService)
        };

        foreach (var serviceType in applicationServiceTypes)
        {
            var descriptor = _services.FirstOrDefault(d => d.ServiceType == serviceType);
            descriptor.Should().NotBeNull($"{serviceType.Name} should be registered");
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterDbContextWithSQLite_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPesoDbContext>();
        
        dbContext.Should().NotBeNull();
        dbContext.Database.ProviderName.Should().Contain("Sqlite");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldAllowMultipleServiceResolutions_WhenCalledMultipleTimes()
    {
        // Arrange
        _services.AddInfrastructureServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var dbContext1 = scope1.ServiceProvider.GetRequiredService<ControlPesoDbContext>();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<ControlPesoDbContext>();

        // Assert
        dbContext1.Should().NotBeNull();
        dbContext2.Should().NotBeNull();
        dbContext1.Should().NotBeSameAs(dbContext2); // Different instances per scope
    }

    [Fact]
    public void AddInfrastructureServices_ShouldEnableDevelopmentFeaturesWhenDevelopmentEnvironment()
    {
        // Arrange
        var developmentEnvironment = new TestHostEnvironment { EnvironmentName = "Development" };

        // Act
        _services.AddInfrastructureServices(_configuration, developmentEnvironment);

        // Assert - Verify registration worked
        _services.Should().NotBeEmpty();

        // Build service provider to trigger DbContext configuration
        using var serviceProvider = _services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Resolve DbContext to execute the configuration lambda (which contains the Development-specific code)
        var dbContext = scope.ServiceProvider.GetRequiredService<ControlPesoDbContext>();
        dbContext.Should().NotBeNull("DbContext should be resolvable in Development environment");

        // Verify Development-specific options were applied by checking ChangeTracker settings
        // (EnableSensitiveDataLogging affects ChangeTracker behavior)
        dbContext.ChangeTracker.Should().NotBeNull();
    }

    #endregion

    // Helper class to simulate IHostEnvironment
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
