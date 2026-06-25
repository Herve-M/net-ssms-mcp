using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ssmsmcp.Server.Mcp.tools;

namespace ssmsmcp.Server.Mcp.Shared;

public static class DependencyInjection
{
    public static IServiceCollection UseMcpSharedLayer(
            [NotNull] this IServiceCollection services
            )
    {
        return services;
    }

    public static IMcpServerBuilder AddTools(this IMcpServerBuilder mcpBuilder)
    {
        return mcpBuilder
            .WithTools<ServerTools>()
            .WithTools<DatabaseTools>()
            ;
    }
}
