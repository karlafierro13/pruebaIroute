namespace Iroute.Commerce.Application.Exceptions;

/// <summary>
/// El recurso solicitado no existe. La API lo traduce a 404 Not Found.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);
