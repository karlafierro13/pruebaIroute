using Iroute.Commerce.Application.Dtos;
using Iroute.Commerce.Application.Models;

namespace Iroute.Commerce.Application.Abstractions;

public interface ICommerceRepository
{
    /// <summary>Invoca sp_create_commerce. Retorna la cantidad de registros insertados.</summary>
    Task<int> CreateAsync(IReadOnlyCollection<CommerceRecord> records, CancellationToken ct);

    /// <summary>Invoca sp_validate_commerce. Retorna la cantidad de registros en cuarentena.</summary>
    Task<int> ValidateAsync(DateOnly processDate, CancellationToken ct);

    Task<IReadOnlyList<QuarantineRecord>> GetQuarantineAsync(DateOnly? processDate, CancellationToken ct);

    Task<IReadOnlyList<ProcessDateSummary>> GetProcessDatesAsync(CancellationToken ct);

    /// <summary>Invoca sp_update_commerce_quarantine (corrige y revalida el registro).</summary>
    Task<UpdateQuarantineResultDto> UpdateQuarantineAsync(long id, UpdateQuarantineDto data, CancellationToken ct);

    /// <summary>Invoca sp_delete_commerce_quarantine (descarta el registro).</summary>
    Task DeleteQuarantineAsync(long id, CancellationToken ct);
}
