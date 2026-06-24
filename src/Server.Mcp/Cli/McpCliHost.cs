using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ssmsmcp.Application;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure;
using ssmsmcp.Server.Mcp.Shared;

namespace ssmsmcp.Server.Mcp.Cli;

internal static class McpCliHost
{
    public static async Task RunAsync(MainConfiguration configuration)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services
            .UseInfrastructureLayer(builder.Configuration, builder.Environment)
                .WithServiceDiscovery()
                .WithOpenTelemetry(cfg =>
                {
                    cfg.EnableMcp();
                })
                .WithFeatureToggle()
                    .WithRuntimeConfiguration(builder.Configuration, configuration)
                .WithSSMS()
                .Build()
            .UseApplicationLayer(builder.Configuration)
            ;

        builder.Services
            .AddMediator(options =>
            {
                options.Namespace = "ssmsmcp.Server.Mcp.Mediator";
                options.ServiceLifetime = ServiceLifetime.Singleton;
                options.GenerateTypesAsInternal = true;
                options.NotificationPublisherType = typeof(ForeachAwaitPublisher);
                options.Assemblies =
                [
                    typeof(ssmsmcp.Application.DependencyInjection).Assembly
                ];
                options.PipelineBehaviors = [];
                options.StreamPipelineBehaviors = [];
            });

        // Add the MCP services: the transport to use (stdio) and the tools to register.
        builder.Services
            .AddMcpServer(cfg =>
            {
                cfg.ServerInfo = new()
                {
                    Name = "SSMS MCP Server",
                    Version = "0.1.0-beta",
                    Description = "MCP server for SQL Server Management Studio integration, providing server metadata and configuration information."
                };
            })
            .WithStdioServerTransport()
            .AddTools();

        await builder.Build().RunAsync();
    }
}
