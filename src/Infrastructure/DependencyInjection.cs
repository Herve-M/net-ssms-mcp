using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ssmsmcp.Infrastructure.Abstractions.Configurations;
using ssmsmcp.Infrastructure.Configurations;

namespace ssmsmcp.Infrastructure;

public static class DependencyInjections
{
    public static IInfrastructureDependencyInjectionBuilder UseInfrastructureLayer(
        [NotNull] this IServiceCollection services,
        [NotNull] IConfiguration configuration,
        [NotNull] IHostEnvironment hostEnvironment
    )
    {
        return new DependencyInjectionBuilder(services, configuration, hostEnvironment);
    }

}
