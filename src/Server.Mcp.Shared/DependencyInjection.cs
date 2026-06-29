using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
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

    public static IServiceCollection AddDefaultServerName(this IServiceCollection services, string? value)
    {
        return services.AddSingleton<IDefaultServerName>(new DefaultServerName(value));
    }

    public static IMcpServerBuilder AddTools(this IMcpServerBuilder mcpBuilder)
    {
        return mcpBuilder
            .WithTools<ServerTools>()
            .WithTools<DatabaseTools>()
            ;
    }
}
