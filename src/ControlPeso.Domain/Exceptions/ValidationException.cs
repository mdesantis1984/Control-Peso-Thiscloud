namespace ControlPeso.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando falla la validación de una entidad o regla de negocio.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException()
    {
    }

    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]>? Errors { get; }
}
