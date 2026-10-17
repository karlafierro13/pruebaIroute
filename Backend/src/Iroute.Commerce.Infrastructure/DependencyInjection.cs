using Dapper;
using Iroute.Commerce.Application.Abstractions;
using Iroute.Commerce.Infrastructure.Persistence;
using Iroute.Commerce.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Iroute.Commerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CommerceDb")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'CommerceDb'.");

        // pc_nomcomred -> PcNomComRed, quarantined_at -> QuarantinedAt
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<ICommerceRepository, CommerceRepository>();
        return services;
    }
}
