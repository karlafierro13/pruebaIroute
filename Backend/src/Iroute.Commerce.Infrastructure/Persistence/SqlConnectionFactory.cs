using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Iroute.Commerce.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct);
}

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
