namespace JsonToCsv.Domain.Exceptions;

/// <summary>Error de negocio/validación que debe llegar al cliente como 400.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
