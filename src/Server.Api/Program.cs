using Asp.Versioning;
using HealthChecks.UI.Client;
using Mediator;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using ssmsmcp.Application;
using ssmsmcp.Infrastructure;
using ssmsmcp.Server.Mcp.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddApiVersioning(options =>
    {
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddMvc()
    .AddOpenApi(options =>
    {
        //TODO: fait for official fix from Scalar?
        // options.AddScalarTransformers();
    })
    ;

builder.Services
    .AddMcpServer(cfg =>
    {
        cfg.ServerInfo = new()
        {
            Name = "SSMS API Server",
            Version = "0.1.0-beta",
            Description = "API server for SQL Server Management Studio integration, providing server metadata and configuration information."
        };
    })
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .AddTools()
    ;
builder.Services.AddDefaultServerName(null);

builder.Services
    .UseInfrastructureLayer(builder.Configuration, builder.Environment)
        .WithOpenTelemetry(cfg =>
        {
            cfg.EnableAspNetCore();
        })
        .WithServiceDiscovery()
        .WithHealthChecks()
        .WithFeatureToggle()
            .WithFileConfiguration(builder.Configuration, "configs/main.json")
        .WithSSMS()
            .Build()
    .UseApplicationLayer(builder.Configuration)
    ;

builder.Services
    .AddMediator((MediatorOptions options) =>
    {
        options.Namespace = "ssmsmcp.Server.Api.Mediator";
        options.ServiceLifetime = ServiceLifetime.Transient;
        options.GenerateTypesAsInternal = true;
        options.NotificationPublisherType = typeof(Mediator.ForeachAwaitPublisher);
        options.Assemblies =
        [
            typeof(ssmsmcp.Application.DependencyInjection).Assembly
        ];
        options.PipelineBehaviors = [];
        options.StreamPipelineBehaviors = [];
    });

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .WithDocumentPerVersion()
        ;

    app.MapScalarApiReference("/docs");
}

app.MapMcp("/mcp");

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
