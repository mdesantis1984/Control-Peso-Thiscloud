using ControlPeso.Domain.Exceptions;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_Default_ShouldCreateException()
    {
        // Act
        var exception = new DomainException();

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test domain error";

        // Act
        var exception = new DomainException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test domain error";
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new DomainException(message, inner);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(inner);
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_Default_ShouldCreateException()
    {
        // Act
        var exception = new NotFoundException();

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Entity not found";

        // Act
        var exception = new NotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Entity not found";
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new NotFoundException(message, inner);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(inner);
    }
}

public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_Default_ShouldCreateException()
    {
        // Act
        var exception = new ValidationException();

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Validation failed";
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ValidationException(message, inner);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(inner);
    }
}
