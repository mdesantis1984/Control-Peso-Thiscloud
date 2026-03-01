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

    [Fact]
    public void Constructor_WithEntityNameAndKey_ShouldFormatMessage()
    {
        // Arrange
        const string entityName = "WeightLog";
        var key = Guid.NewGuid();

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Contain(entityName);
        exception.Message.Should().Contain(key.ToString());
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
    }

    [Fact]
    public void Constructor_WithEntityNameAndStringKey_ShouldFormatMessage()
    {
        // Arrange
        const string entityName = "User";
        const string key = "user123";

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Contain(entityName);
        exception.Message.Should().Contain(key);
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
    }

    [Fact]
    public void Constructor_WithEntityNameAndIntKey_ShouldFormatMessage()
    {
        // Arrange
        const string entityName = "Product";
        const int key = 12345;

        // Act
        var exception = new NotFoundException(entityName, key);

        // Assert
        exception.Message.Should().Contain(entityName);
        exception.Message.Should().Contain(key.ToString());
        exception.Message.Should().Be($"Entity \"{entityName}\" ({key}) was not found.");
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
        exception.Errors.Should().BeNull();
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
        exception.Errors.Should().BeNull();
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
        exception.Errors.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithErrors_ShouldSetErrorsAndDefaultMessage()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Weight"] = new[] { "Weight must be between 20 and 500 kg" },
            ["Date"] = new[] { "Date cannot be in the future" }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Be("One or more validation errors occurred.");
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().ContainKey("Weight");
        exception.Errors.Should().ContainKey("Date");
    }

    [Fact]
    public void Constructor_WithErrors_ShouldPreserveAllErrorMessages()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required", "Email must be valid" },
            ["Password"] = new[] { "Password is required", "Password must be at least 8 characters" }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().NotBeNull();
        exception.Errors!["Email"].Should().HaveCount(2);
        exception.Errors["Email"].Should().Contain("Email is required");
        exception.Errors["Email"].Should().Contain("Email must be valid");
        exception.Errors["Password"].Should().HaveCount(2);
        exception.Errors["Password"].Should().Contain("Password is required");
        exception.Errors["Password"].Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public void Constructor_WithEmptyErrors_ShouldCreateExceptionWithEmptyDictionary()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>();

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Be("One or more validation errors occurred.");
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithSingleFieldError_ShouldSetError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Username"] = new[] { "Username is already taken" }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().HaveCount(1);
        exception.Errors!["Username"].Should().ContainSingle();
        exception.Errors["Username"][0].Should().Be("Username is already taken");
    }

    [Fact]
    public void Errors_Property_WhenConstructedWithoutErrors_ShouldBeNull()
    {
        // Act
        var exception1 = new ValidationException();
        var exception2 = new ValidationException("Validation failed");
        var exception3 = new ValidationException("Validation failed", new Exception());

        // Assert
        exception1.Errors.Should().BeNull();
        exception2.Errors.Should().BeNull();
        exception3.Errors.Should().BeNull();
    }

    [Fact]
    public void Errors_Property_WhenConstructedWithErrors_ShouldNotBeNull()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Field"] = new[] { "Error" }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().NotBeNull();
    }
}
