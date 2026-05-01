using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ssmsmcp.Domain;

namespace ssmsmcp.Application;

public static class DependencyInjection
{
    public static IServiceCollection UseApplicationLayer(
            [NotNull] this IServiceCollection services,
            [NotNull] IConfiguration configuration
            )
    {
        services
            .UseDomainLayer();

        return services;
    }
}
