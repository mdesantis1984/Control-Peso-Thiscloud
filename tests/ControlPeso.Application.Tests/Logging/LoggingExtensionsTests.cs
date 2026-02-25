using ControlPeso.Application.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ControlPeso.Application.Tests.Logging;

/// <summary>
/// Tests unitarios para LoggingExtensions - extension methods para scopes estructurados.
/// </summary>
public sealed class LoggingExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public LoggingExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void BeginBusinessScope_ShouldCreateScopeWithCorrectProperties()
    {
        // Arrange
        const string operation = "CreateWeightLog";
        Dictionary<string, object>? capturedState = null;

        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Callback<Dictionary<string, object>>(state => capturedState = state)
            .Returns(Mock.Of<IDisposable>());

        // Act
        var scope = _mockLogger.Object.BeginBusinessScope(operation);

        // Assert
        Assert.NotNull(scope);
        Assert.NotNull(capturedState);
        Assert.Equal("Business", capturedState!["LogType"]);
        Assert.Equal(operation, capturedState["Operation"]);
    }

    [Fact]
    public void BeginInfrastructureScope_ShouldCreateScopeWithCorrectProperties()
    {
        // Arrange
        const string operation = "DatabaseQuery";
        Dictionary<string, object>? capturedState = null;

        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Callback<Dictionary<string, object>>(state => capturedState = state)
            .Returns(Mock.Of<IDisposable>());

        // Act
        var scope = _mockLogger.Object.BeginInfrastructureScope(operation);

        // Assert
        Assert.NotNull(scope);
        Assert.NotNull(capturedState);
        Assert.Equal("Infrastructure", capturedState!["LogType"]);
        Assert.Equal(operation, capturedState["Operation"]);
    }

    [Fact]
    public void BeginSecurityScope_ShouldCreateScopeWithCorrectProperties()
    {
        // Arrange
        const string operation = "GoogleOAuthCallback";
        Dictionary<string, object>? capturedState = null;

        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Callback<Dictionary<string, object>>(state => capturedState = state)
            .Returns(Mock.Of<IDisposable>());

        // Act
        var scope = _mockLogger.Object.BeginSecurityScope(operation);

        // Assert
        Assert.NotNull(scope);
        Assert.NotNull(capturedState);
        Assert.Equal("Security", capturedState!["LogType"]);
        Assert.Equal(operation, capturedState["Operation"]);
    }

    [Fact]
    public void BeginBusinessScope_WithNullLogger_ShouldNotThrow()
    {
        // Arrange
        ILogger nullLogger = NullLogger.Instance;

        // Act & Assert - no debe lanzar excepción
        var scope = nullLogger.BeginBusinessScope("TestOperation");
        
        // NullLogger puede retornar null en BeginScope
        Assert.True(scope == null || scope is IDisposable);
    }

    [Fact]
    public void BeginInfrastructureScope_WithNullLogger_ShouldNotThrow()
    {
        // Arrange
        ILogger nullLogger = NullLogger.Instance;

        // Act & Assert - no debe lanzar excepción
        var scope = nullLogger.BeginInfrastructureScope("TestOperation");
        
        Assert.True(scope == null || scope is IDisposable);
    }

    [Fact]
    public void BeginSecurityScope_WithNullLogger_ShouldNotThrow()
    {
        // Arrange
        ILogger nullLogger = NullLogger.Instance;

        // Act & Assert - no debe lanzar excepción
        var scope = nullLogger.BeginSecurityScope("TestOperation");
        
        Assert.True(scope == null || scope is IDisposable);
    }

    [Fact]
    public void BeginBusinessScope_ShouldReturnDisposableScope()
    {
        // Arrange
        var mockDisposable = new Mock<IDisposable>();
        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Returns(mockDisposable.Object);

        // Act
        using (var scope = _mockLogger.Object.BeginBusinessScope("TestOp"))
        {
            Assert.NotNull(scope);
        }

        // Assert - Dispose debe haber sido llamado
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void BeginInfrastructureScope_ShouldReturnDisposableScope()
    {
        // Arrange
        var mockDisposable = new Mock<IDisposable>();
        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Returns(mockDisposable.Object);

        // Act
        using (var scope = _mockLogger.Object.BeginInfrastructureScope("TestOp"))
        {
            Assert.NotNull(scope);
        }

        // Assert - Dispose debe haber sido llamado
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void BeginSecurityScope_ShouldReturnDisposableScope()
    {
        // Arrange
        var mockDisposable = new Mock<IDisposable>();
        _mockLogger
            .Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
            .Returns(mockDisposable.Object);

        // Act
        using (var scope = _mockLogger.Object.BeginSecurityScope("TestOp"))
        {
            Assert.NotNull(scope);
        }

        // Assert - Dispose debe haber sido llamado
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }
}
