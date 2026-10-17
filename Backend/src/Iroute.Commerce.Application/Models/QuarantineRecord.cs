namespace Iroute.Commerce.Application.Models;

public sealed record QuarantineRecord
{
    public long Id { get; init; }
    public long CommerceId { get; init; }
    public DateOnly PcProcessDate { get; init; }
    public string? PcCodComRed { get; init; }
    public string? PcNomComRed { get; init; }
    public string? PcTipDoc { get; init; }
    public string? PcNumDoc { get; init; }
    public string? PcDireccion { get; init; }
    public string? PcCiudad { get; init; }
    public string? PcTelefono { get; init; }
    public string? PcEmail { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public DateTime QuarantinedAt { get; init; }
}
