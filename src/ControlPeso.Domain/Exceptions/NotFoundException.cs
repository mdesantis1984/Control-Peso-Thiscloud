namespace ControlPeso.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando no se encuentra una entidad solicitada.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }
}
