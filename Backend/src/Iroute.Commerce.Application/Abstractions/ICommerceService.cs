using Iroute.Commerce.Application.Dtos;
using Iroute.Commerce.Application.Models;

namespace Iroute.Commerce.Application.Abstractions;

public interface ICommerceService
{
    Task<UploadResultDto> UploadAsync(string fileName, Stream content, CancellationToken ct);

    Task<ProcessResultDto> ProcessAsync(DateOnly processDate, CancellationToken ct);

    Task<IReadOnlyList<QuarantineRecord>> GetQuarantineAsync(DateOnly? processDate, CancellationToken ct);

    Task<IReadOnlyList<ProcessDateSummary>> GetProcessDatesAsync(CancellationToken ct);

    Task<UpdateQuarantineResultDto> UpdateQuarantineAsync(long id, UpdateQuarantineDto data, CancellationToken ct);

    Task DeleteQuarantineAsync(long id, CancellationToken ct);
}
