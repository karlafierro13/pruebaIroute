namespace Iroute.Commerce.Application.Models;

/// <summary>
/// Fila del archivo commerce_DDMMYYYY.csv (misma estructura que la tabla commerce).
/// </summary>
public sealed record CommerceRecord
{
    public DateOnly PcProcessDate { get; init; }
    public string? PcCodComRed { get; init; }
    public string? PcNomComRed { get; init; }
    public string? PcTipDoc { get; init; }
    public string? PcNumDoc { get; init; }
    public string? PcDireccion { get; init; }
    public string? PcCiudad { get; init; }
    public string? PcTelefono { get; init; }
    public string? PcEmail { get; init; }
}
