using Iroute.Commerce.Application.Exceptions;
using Microsoft.Data.SqlClient;

namespace Iroute.Commerce.Infrastructure.Persistence;

/// <summary>
/// Traduce los errores de negocio que lanzan los stored procedures (THROW 5000x)
/// a excepciones de la capa Application, conservando el mensaje del SP.
/// </summary>
internal static class SqlBusinessErrors
{
    private const int DateAlreadyLoaded = 50001;
    private const int DuplicatedInFile = 50002;
    private const int AlreadyRegistered = 50003;
    private const int NotFound = 50004;
    private const int LoadInProgress = 50005;
    private const int MissingCode = 50006;

    public static async Task<T> TranslateAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (SqlException ex) when (Map(ex) is { } translated)
        {
            throw translated;
        }
    }

    private static Exception? Map(SqlException ex) => ex.Number switch
    {
        DateAlreadyLoaded or AlreadyRegistered or LoadInProgress => new ConflictException(ex.Message),
        DuplicatedInFile or MissingCode => new ValidationException(ex.Message),
        NotFound => new NotFoundException(ex.Message),
        _ => null
    };
}
