using ControlPeso.Shared.Resources.Localization;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ControlPeso.Shared.Resources.Tests.Localization;

public class SharedResourceStringLocalizerFactoryTests
{
    private static SharedResourceStringLocalizerFactory CreateFactory(
        Mock<ILoggerFactory>? loggerFactoryMock = null,
        LocalizationOptions? options = null)
    {
        loggerFactoryMock ??= new Mock<ILoggerFactory>();

        // Only setup if no setup exists (to allow test-specific setup)
        if (loggerFactoryMock.Setups.Count == 0)
        {
            loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>().Object);
        }

        options ??= new LocalizationOptions();
        var optionsMock = new Mock<IOptions<LocalizationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        return new SharedResourceStringLocalizerFactory(
            optionsMock.Object,
            loggerFactoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLocalizationOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerFactory = new Mock<ILoggerFactory>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SharedResourceStringLocalizerFactory(null!, loggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new Mock<IOptions<LocalizationOptions>>();
        options.Setup(o => o.Value).Returns(new LocalizationOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SharedResourceStringLocalizerFactory(options.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidArguments_ShouldNotThrow()
    {
        // Act
        var factory = CreateFactory();

        // Assert
        factory.Should().NotBeNull();
    }

    #endregion

    #region Create(Type) Tests

    [Fact]
    public void Create_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            factory.Create(null!));
    }

    [Fact]
    public void Create_WithValidType_ShouldReturnLocalizer()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create(typeof(SharedResourceStringLocalizerFactoryTests));

        // Assert
        localizer.Should().NotBeNull();
        localizer.Should().BeAssignableTo<IStringLocalizer>();
    }

    [Fact]
    public void Create_WithSameTypeTwice_ShouldReturnCachedInstance()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer1 = factory.Create(typeof(SharedResourceStringLocalizerFactoryTests));
        var localizer2 = factory.Create(typeof(SharedResourceStringLocalizerFactoryTests));

        // Assert
        localizer1.Should().BeSameAs(localizer2);
    }

    [Fact]
    public void Create_WithDifferentTypes_ShouldReturnDifferentLocalizers()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer1 = factory.Create(typeof(SharedResourceStringLocalizerFactoryTests));
        var localizer2 = factory.Create(typeof(SharedResourceStringLocalizerFactory));

        // Assert
        localizer1.Should().NotBeSameAs(localizer2);
    }

    #endregion

    #region Create(string, string) Tests

    [Fact]
    public void Create_WithNullBaseName_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() =>
            factory.Create(null!, "SomeLocation"));
    }

    [Fact]
    public void Create_WithEmptyBaseName_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            factory.Create(string.Empty, "SomeLocation"));
    }

    [Fact]
    public void Create_WithNullLocation_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        Assert.Throws<ArgumentNullException>(() =>
            factory.Create("BaseName", null!));
    }

    [Fact]
    public void Create_WithEmptyLocation_ShouldThrowArgumentException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            factory.Create("BaseName", string.Empty));
    }

    [Fact]
    public void Create_WithValidArguments_ShouldReturnLocalizer()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("Home", "ControlPeso.Web.Pages");

        // Assert
        localizer.Should().NotBeNull();
        localizer.Should().BeAssignableTo<IStringLocalizer>();
    }

    [Fact]
    public void Create_WithWebComponentsNamespace_ShouldMapCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("Home", "ControlPeso.Web.Components.Pages");

        // Assert
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithApplicationValidatorsNamespace_ShouldMapCorrectly()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("CreateWeightLogValidator", "ControlPeso.Application.Validators");

        // Assert
        localizer.Should().NotBeNull();
    }

    #endregion

    #region Namespace Mapping Tests

    [Fact]
    public void Create_WithWebPagesNamespace_ShouldMapToPages()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("Dashboard", "ControlPeso.Web.Pages");

        // Assert
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithWebComponentsLayoutNamespace_ShouldMapToComponentsLayout()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("MainLayout", "ControlPeso.Web.Components.Layout");

        // Assert
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithWebComponentsSharedNamespace_ShouldMapToComponentsShared()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var localizer = factory.Create("NavMenu", "ControlPeso.Web.Components.Shared");

        // Assert
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithUnknownNamespace_ShouldMapToRoot()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var factory = CreateFactory(loggerFactoryMock);

        // Act
        var localizer = factory.Create("SomeComponent", "Unknown.Namespace");

        // Assert
        localizer.Should().NotBeNull();

        // Verify warning was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No specific mapping found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
