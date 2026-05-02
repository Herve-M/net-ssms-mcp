using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace ssmsmcp.Domain;

public static class DependencyInjection
{
    public static IServiceCollection UseDomainLayer(
            [NotNull] this IServiceCollection services
            )
    {
        return services;
    }
}
