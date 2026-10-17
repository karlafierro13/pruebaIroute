using Iroute.Commerce.Application.Abstractions;
using Iroute.Commerce.Application.Csv;
using Iroute.Commerce.Application.Dtos;
using Iroute.Commerce.Application.Exceptions;
using Iroute.Commerce.Application.Models;
using Microsoft.Extensions.Logging;

namespace Iroute.Commerce.Application.Services;

public sealed class CommerceService(
    ICommerceRepository repository,
    ICommerceCsvParser parser,
    ILogger<CommerceService> logger) : ICommerceService
{
    public async Task<UploadResultDto> UploadAsync(string fileName, Stream content, CancellationToken ct)
    {
        if (!CommerceFileName.TryGetDate(fileName, out var fileDate))
            throw new ValidationException("El nombre del archivo debe tener el formato commerce_DDMMYYYY.csv.");

        var records = await parser.ParseAsync(content, fileDate, ct);
        var inserted = await repository.CreateAsync(records, ct);

        logger.LogInformation("Archivo {FileName} cargado: {Inserted} registros", fileName, inserted);
        return new UploadResultDto(fileName, inserted);
    }

    public async Task<ProcessResultDto> ProcessAsync(DateOnly processDate, CancellationToken ct)
    {
        var quarantined = await repository.ValidateAsync(processDate, ct);

        logger.LogInformation("Proceso {ProcessDate}: {Quarantined} registros en cuarentena", processDate, quarantined);
        return new ProcessResultDto(processDate, quarantined);
    }

    public Task<IReadOnlyList<QuarantineRecord>> GetQuarantineAsync(DateOnly? processDate, CancellationToken ct)
        => repository.GetQuarantineAsync(processDate, ct);

    public Task<IReadOnlyList<ProcessDateSummary>> GetProcessDatesAsync(CancellationToken ct)
        => repository.GetProcessDatesAsync(ct);

    public async Task<UpdateQuarantineResultDto> UpdateQuarantineAsync(long id, UpdateQuarantineDto data, CancellationToken ct)
    {
        var result = await repository.UpdateQuarantineAsync(id, data, ct);

        logger.LogInformation("Cuarentena {Id} corregida. Devuelto a commerce: {Released}", id, result.Released);
        return result;
    }

    public async Task DeleteQuarantineAsync(long id, CancellationToken ct)
    {
        await repository.DeleteQuarantineAsync(id, ct);
        logger.LogInformation("Cuarentena {Id} descartada", id);
    }
}
