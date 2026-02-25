using ControlPeso.Shared.Resources.Localization;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace ControlPeso.Shared.Resources.Tests.Localization;

public class FactoryStringLocalizerTests
{
    [Fact]
    public void Constructor_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FactoryStringLocalizer<FactoryStringLocalizerTests>(null!));
    }

    [Fact]
    public void Constructor_WithValidFactory_ShouldNotThrow()
    {
        // Arrange
        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(new Mock<IStringLocalizer>().Object);

        // Act
        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Assert
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCallFactoryCreateWithCorrectType()
    {
        // Arrange
        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(new Mock<IStringLocalizer>().Object);

        // Act
        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Assert
        factoryMock.Verify(f => f.Create(typeof(FactoryStringLocalizerTests)), Times.Once);
    }

    [Fact]
    public void Indexer_ShouldDelegateToUnderlyingLocalizer()
    {
        // Arrange
        var underlyingMock = new Mock<IStringLocalizer>();
        underlyingMock.Setup(l => l["TestKey"])
            .Returns(new LocalizedString("TestKey", "TestValue"));

        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(underlyingMock.Object);

        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Act
        var result = localizer["TestKey"];

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestKey");
        result.Value.Should().Be("TestValue");
    }

    [Fact]
    public void IndexerWithArguments_ShouldDelegateToUnderlyingLocalizer()
    {
        // Arrange
        var underlyingMock = new Mock<IStringLocalizer>();
        underlyingMock.Setup(l => l["Hello {0}", "World"])
            .Returns(new LocalizedString("Hello {0}", "Hello World"));

        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(underlyingMock.Object);

        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Act
        var result = localizer["Hello {0}", "World"];

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Hello {0}");
        result.Value.Should().Be("Hello World");
    }

    [Fact]
    public void GetAllStrings_ShouldDelegateToUnderlyingLocalizer()
    {
        // Arrange
        var expectedStrings = new List<LocalizedString>
        {
            new LocalizedString("Key1", "Value1"),
            new LocalizedString("Key2", "Value2")
        };

        var underlyingMock = new Mock<IStringLocalizer>();
        underlyingMock.Setup(l => l.GetAllStrings(It.IsAny<bool>()))
            .Returns(expectedStrings);

        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(underlyingMock.Object);

        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Act
        var result = localizer.GetAllStrings(includeParentCultures: true).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Name == "Key1" && s.Value == "Value1");
        result.Should().Contain(s => s.Name == "Key2" && s.Value == "Value2");
    }

    [Fact]
    public void GetAllStrings_ShouldPassIncludeParentCulturesParameter()
    {
        // Arrange
        var underlyingMock = new Mock<IStringLocalizer>();
        underlyingMock.Setup(l => l.GetAllStrings(It.IsAny<bool>()))
            .Returns(new List<LocalizedString>());

        var factoryMock = new Mock<IStringLocalizerFactory>();
        factoryMock.Setup(f => f.Create(It.IsAny<Type>()))
            .Returns(underlyingMock.Object);

        var localizer = new FactoryStringLocalizer<FactoryStringLocalizerTests>(factoryMock.Object);

        // Act
        localizer.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        underlyingMock.Verify(l => l.GetAllStrings(false), Times.Once);
    }
}
