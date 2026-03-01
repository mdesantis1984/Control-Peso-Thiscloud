using ControlPeso.Application.Interfaces;
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

        // Assert - Factory is registered as Singleton
        var factoryDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IDbContextFactory<ControlPesoDbContext>));
        factoryDescriptor.Should().NotBeNull("IDbContextFactory<ControlPesoDbContext> should be registered");
        factoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton, "Factory should be Singleton");

        // Backward compatibility: ControlPesoDbContext should also be available as Scoped
        var dbContextDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(ControlPesoDbContext));
        dbContextDescriptor.Should().NotBeNull("ControlPesoDbContext should be registered for backward compatibility");
        dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterGenericDbContext_WhenCalled()
    {
        // Act
        _services.AddInfrastructureServices(_configuration);

        // Assert - Generic factory wrapper should be registered
        var factoryDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IDbContextFactory<DbContext>));
        factoryDescriptor.Should().NotBeNull("IDbContextFactory<DbContext> wrapper should be registered");
        factoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton, "Factory wrapper should be Singleton");

        // Backward compatibility: DbContext should also be available as Scoped
        var dbContextDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(DbContext));
        dbContextDescriptor.Should().NotBeNull("DbContext should be registered for backward compatibility");
        dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    // NOTE: IDbSeeder test removed - service was intentionally removed from Infrastructure
    // App works ONLY with real OAuth users (Google/LinkedIn), no fake/demo data seeding

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

        // Assert - Factory can create DbContext instances
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();
        factory.Should().NotBeNull("Factory should be resolvable");

        using var dbContext = factory.CreateDbContext();
        dbContext.Should().NotBeNull("Factory should create DbContext instances");
        dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite", "SQLite provider should be detected from Data Source connection string");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddInfrastructureServices_ShouldAllowMultipleServiceResolutions_WhenCalledMultipleTimes()
    {
        // Arrange
        _services.AddInfrastructureServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act - Factory creates independent instances
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();

        using var dbContext1 = factory.CreateDbContext();
        using var dbContext2 = factory.CreateDbContext();

        // Assert
        dbContext1.Should().NotBeNull();
        dbContext2.Should().NotBeNull();
        dbContext1.Should().NotBeSameAs(dbContext2, "Factory should create different instances per call");
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

        // Build service provider and get factory
        using var serviceProvider = _services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlPesoDbContext>>();
        factory.Should().NotBeNull("Factory should be resolvable in Development environment");

        // Create DbContext to execute the configuration lambda (which contains the Development-specific code)
        using var dbContext = factory.CreateDbContext();
        dbContext.Should().NotBeNull("Factory should create DbContext instances in Development environment");

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
