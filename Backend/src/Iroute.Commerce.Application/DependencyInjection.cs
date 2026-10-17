using Iroute.Commerce.Application.Abstractions;
using Iroute.Commerce.Application.Csv;
using Iroute.Commerce.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Iroute.Commerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICommerceCsvParser, CommerceCsvParser>();
        services.AddScoped<ICommerceService, CommerceService>();
        return services;
    }
}
