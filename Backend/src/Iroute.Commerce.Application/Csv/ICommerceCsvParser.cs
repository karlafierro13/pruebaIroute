using Iroute.Commerce.Application.Models;

namespace Iroute.Commerce.Application.Csv;

public interface ICommerceCsvParser
{
    /// <summary>
    /// Convierte el contenido CSV en registros. Lanza ValidationException si la
    /// estructura del archivo es inválida (cabecera, columnas, fechas, longitudes),
    /// si alguna fila no corresponde a <paramref name="expectedProcessDate"/> o si
    /// se repite un código de comercio.
    /// </summary>
    Task<IReadOnlyList<CommerceRecord>> ParseAsync(Stream content, DateOnly expectedProcessDate, CancellationToken ct);
}
