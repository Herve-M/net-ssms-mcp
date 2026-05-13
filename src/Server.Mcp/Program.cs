using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ssmsmcp.Application;
using ssmsmcp.Infrastructure;
using ssmsmcp.Server.Mcp.Shared;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .UseInfrastructureLayer(builder.Configuration, builder.Environment)
        .WithOpenTelemetry(cfg =>
        {
            cfg.EnableMcp();
        })
        .WithServiceDiscovery()
        .WithHealthChecks()
        .WithFeatureToggle()
            .WithRuntimeConfiguration(builder.Configuration, "configs/main.json")
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
