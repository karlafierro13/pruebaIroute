namespace Iroute.Commerce.Application.Exceptions;

/// <summary>
/// La operación entra en conflicto con datos existentes (p. ej. comercios duplicados).
/// La API lo traduce a 409 Conflict.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
