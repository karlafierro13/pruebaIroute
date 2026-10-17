using System.ComponentModel.DataAnnotations;

namespace Iroute.Commerce.Application.Dtos;

public sealed record UploadResultDto(string FileName, int InsertedCount);

public sealed record ProcessRequestDto(DateOnly ProcessDate);

public sealed record ProcessResultDto(DateOnly ProcessDate, int QuarantinedCount);

/// <summary>
/// Datos corregibles de un registro en cuarentena. pc_processdate y pc_codcomred
/// no se editan porque identifican al comercio.
/// </summary>
public sealed record UpdateQuarantineDto(
    [MaxLength(150)] string? PcNomComRed,
    [MaxLength(10)] string? PcTipDoc,
    [MaxLength(20)] string? PcNumDoc,
    [MaxLength(250)] string? PcDireccion,
    [MaxLength(100)] string? PcCiudad,
    [MaxLength(20)] string? PcTelefono,
    [MaxLength(150)] string? PcEmail);

/// <summary>
/// Released = true: el registro quedó válido y regresó a commerce.
/// Released = false: sigue en cuarentena con el motivo indicado.
/// </summary>
public sealed record UpdateQuarantineResultDto(bool Released, string? Motivo);
