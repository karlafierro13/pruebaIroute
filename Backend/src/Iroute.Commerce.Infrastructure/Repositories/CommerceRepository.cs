using System.Data;
using Dapper;
using Iroute.Commerce.Application.Abstractions;
using Iroute.Commerce.Application.Dtos;
using Iroute.Commerce.Application.Models;
using Iroute.Commerce.Infrastructure.Persistence;

namespace Iroute.Commerce.Infrastructure.Repositories;

public sealed class CommerceRepository(IDbConnectionFactory connectionFactory) : ICommerceRepository
{
    private const int CommandTimeoutSeconds = 120;

    public async Task<int> CreateAsync(IReadOnlyCollection<CommerceRecord> records, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        // Todas las filas viajan en un único Table-Valued Parameter.
        var parameters = new DynamicParameters();
        parameters.Add("@commerce", ToDataTable(records).AsTableValuedParameter("dbo.commerce_type"));

        return await SqlBusinessErrors.TranslateAsync(() => connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "dbo.sp_create_commerce", parameters,
            commandType: CommandType.StoredProcedure, commandTimeout: CommandTimeoutSeconds, cancellationToken: ct)));
    }

    public async Task<UpdateQuarantineResultDto> UpdateQuarantineAsync(long id, UpdateQuarantineDto data, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@id", id);
        parameters.Add("@pc_nomcomred", data.PcNomComRed);
        parameters.Add("@pc_tipdoc", data.PcTipDoc);
        parameters.Add("@pc_numdoc", data.PcNumDoc);
        parameters.Add("@pc_direccion", data.PcDireccion);
        parameters.Add("@pc_ciudad", data.PcCiudad);
        parameters.Add("@pc_telefono", data.PcTelefono);
        parameters.Add("@pc_email", data.PcEmail);

        return await SqlBusinessErrors.TranslateAsync(() => connection.QuerySingleAsync<UpdateQuarantineResultDto>(
            new CommandDefinition("dbo.sp_update_commerce_quarantine", parameters,
                commandType: CommandType.StoredProcedure, cancellationToken: ct)));
    }

    public async Task DeleteQuarantineAsync(long id, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        await SqlBusinessErrors.TranslateAsync(() => connection.ExecuteAsync(new CommandDefinition(
            "dbo.sp_delete_commerce_quarantine", new { id },
            commandType: CommandType.StoredProcedure, cancellationToken: ct)));
    }

    public async Task<int> ValidateAsync(DateOnly processDate, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@processdate", processDate.ToDateTime(TimeOnly.MinValue), DbType.Date);
        parameters.Add("@quarantined_count", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(new CommandDefinition(
            "dbo.sp_validate_commerce", parameters,
            commandType: CommandType.StoredProcedure, commandTimeout: CommandTimeoutSeconds, cancellationToken: ct));

        return parameters.Get<int?>("@quarantined_count") ?? 0;
    }

    public async Task<IReadOnlyList<QuarantineRecord>> GetQuarantineAsync(DateOnly? processDate, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var parameters = new DynamicParameters();
        parameters.Add("@processdate", processDate?.ToDateTime(TimeOnly.MinValue), DbType.Date);

        var rows = await connection.QueryAsync<QuarantineRecord>(new CommandDefinition(
            "dbo.sp_list_commerce_quarantine", parameters,
            commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return rows.AsList();
    }

    public async Task<IReadOnlyList<ProcessDateSummary>> GetProcessDatesAsync(CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        var rows = await connection.QueryAsync<ProcessDateSummary>(new CommandDefinition(
            "dbo.sp_list_commerce_dates", commandType: CommandType.StoredProcedure, cancellationToken: ct));

        return rows.AsList();
    }

    private static DataTable ToDataTable(IEnumerable<CommerceRecord> records)
    {
        // El orden de las columnas debe coincidir con dbo.commerce_type.
        var table = new DataTable();
        table.Columns.Add("pc_processdate", typeof(DateTime));
        table.Columns.Add("pc_codcomred", typeof(string));
        table.Columns.Add("pc_nomcomred", typeof(string));
        table.Columns.Add("pc_tipdoc", typeof(string));
        table.Columns.Add("pc_numdoc", typeof(string));
        table.Columns.Add("pc_direccion", typeof(string));
        table.Columns.Add("pc_ciudad", typeof(string));
        table.Columns.Add("pc_telefono", typeof(string));
        table.Columns.Add("pc_email", typeof(string));

        foreach (var r in records)
        {
            table.Rows.Add(
                r.PcProcessDate.ToDateTime(TimeOnly.MinValue),
                (object?)r.PcCodComRed ?? DBNull.Value,
                (object?)r.PcNomComRed ?? DBNull.Value,
                (object?)r.PcTipDoc ?? DBNull.Value,
                (object?)r.PcNumDoc ?? DBNull.Value,
                (object?)r.PcDireccion ?? DBNull.Value,
                (object?)r.PcCiudad ?? DBNull.Value,
                (object?)r.PcTelefono ?? DBNull.Value,
                (object?)r.PcEmail ?? DBNull.Value);
        }

        return table;
    }
}
