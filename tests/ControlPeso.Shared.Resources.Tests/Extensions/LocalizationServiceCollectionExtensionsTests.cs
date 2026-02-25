using ControlPeso.Shared.Resources.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ControlPeso.Shared.Resources.Tests.Extensions;

public class LocalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharedResourcesLocalization_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ControlPeso.Shared.Resources.Extensions.LocalizationServiceCollectionExtensions.AddSharedResourcesLocalization(null!));
    }

    [Fact]
    public void AddSharedResourcesLocalization_WithoutOptions_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSharedResourcesLocalization();

        // Assert
        var provider = services.BuildServiceProvider();
        
        var factory = provider.GetService<IStringLocalizerFactory>();
        factory.Should().NotBeNull();
        
        var localizer = provider.GetService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>();
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void AddSharedResourcesLocalization_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSharedResourcesLocalization(options =>
        {
            options.ResourcesPath = "CustomResources";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<LocalizationOptions>>();
        
        options.Should().NotBeNull();
        options!.Value.ResourcesPath.Should().Be("CustomResources");
    }

    [Fact]
    public void AddSharedResourcesLocalization_ShouldRegisterFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSharedResourcesLocalization();

        // Assert
        var provider = services.BuildServiceProvider();
        var factory1 = provider.GetService<IStringLocalizerFactory>();
        var factory2 = provider.GetService<IStringLocalizerFactory>();
        
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void AddSharedResourcesLocalization_ShouldRegisterGenericLocalizerAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSharedResourcesLocalization();

        // Assert
        var provider = services.BuildServiceProvider();
        var localizer1 = provider.GetService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>();
        var localizer2 = provider.GetService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>();
        
        localizer1.Should().NotBeSameAs(localizer2);
    }

    [Fact]
    public void AddSharedResourcesLocalization_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddSharedResourcesLocalization();

        // Assert
        result.Should().BeSameAs(services);
    }
}
