using Asp.Versioning;
using HealthChecks.UI.Client;
using Mediator;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using ssmsmcp.Infrastructure;
using ssmsmcp.Application;

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
        // options.AddScalarTransformers();
    })
    ;

builder.Services
    .UseInfrastructureLayer(builder.Configuration, builder.Environment)
        .WithOpenTelemetry(cfg =>
        {
            cfg.EnableAspNetCore();
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
    .AddMediator((MediatorOptions options) =>
    {
        options.Namespace = "ssmsmcp.Server.Api.Mediator";
        options.ServiceLifetime = ServiceLifetime.Transient;
        options.GenerateTypesAsInternal = true;
        options.NotificationPublisherType = typeof(Mediator.ForeachAwaitPublisher);
        options.Assemblies =
        [
            typeof(ssmsmcp.Application.DependencyInjection).Assembly
            // typeof(ssmsmcp.Infrastructure.DependencyInjection).Assembly
        ];
        options.PipelineBehaviors = [];
        options.StreamPipelineBehaviors = [];
    });

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .WithDocumentPerVersion()
        ;

    app.MapScalarApiReference("/docs");
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
