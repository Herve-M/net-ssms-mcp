using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ssmsmcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection UseDomainLayer(
            [NotNull] this IServiceCollection services,
            [NotNull] IConfiguration configuration,
            [NotNull] IHostEnvironment hostEnvironment
            )
    {
        return services;
    }
}
