namespace Iroute.Commerce.Application.Exceptions;

/// <summary>
/// Error de validación de negocio. La API lo traduce a 400 Bad Request.
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message, IEnumerable<string>? errors = null)
        : base(message)
    {
        Errors = errors?.ToList() ?? [];
    }
}
