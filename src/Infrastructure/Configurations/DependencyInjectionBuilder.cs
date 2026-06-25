using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ssmsmcp.Domain.Abstractions.Configurations;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Servers;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.SSMS;
using ssmsmcp.Infrastructure.SSMS.Internals;

namespace ssmsmcp.Infrastructure.Configurations;

public sealed class DependencyInjectionBuilder(
    IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
    : InfrastructureDependencyInjectionBuilder(configuration, environment, services)
{

    public override IInfrastructureDependencyInjectionBuilder WithOpenTelemetry(Action<OpenTelemetrySettings>? configure = null)
    {
        OpenTelemetrySettings settings = new OpenTelemetrySettings();
        configure?.Invoke(settings);

        Services
            .AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddRuntimeInstrumentation();

                    if (settings.WithAspNetCore)
                    {
                        metrics.AddAspNetCoreInstrumentation();
                    }

                    metrics.AddSqlClientInstrumentation();

                    if (settings.WithMcp)
                    {
                        metrics.AddMeter("Experimental.ModelContextProtocol");
                    }
                })
                .WithTracing(tracing =>
                {
                    if (Environment.IsDevelopment())
                    {
                        tracing.SetSampler(new AlwaysOnSampler());
                    }
                    if (settings.WithAspNetCore)
                    {
                        tracing.AddAspNetCoreInstrumentation();
                    }

                    tracing.AddSqlClientInstrumentation();

                    if (settings.WithMcp)
                    {
                        tracing.AddSource("Experimental.ModelContextProtocol");
                    }
                })
            ;

        bool useOtlpExporter = !string.IsNullOrWhiteSpace(Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            Services.Configure<OpenTelemetryLoggerOptions>(
                logging => logging.AddOtlpExporter());
            Services.ConfigureOpenTelemetryMeterProvider(
                metrics => metrics.AddOtlpExporter());
            Services.ConfigureOpenTelemetryTracerProvider(
                tracing => tracing.AddOtlpExporter());
        }

        return this;
    }

    public override IInfrastructureDependencyInjectionBuilder WithServiceDiscovery()
    {
        Services.AddServiceDiscovery();

        return this;
    }

    public override IInfrastructureDependencyInjectionBuilder WithFeatureToggle()
    {
        Services.AddFeatureManagement();

        return this;
    }

    public override IInfrastructureDependencyInjectionBuilder WithHealthChecks()
    {
        EnableHeathChecks = true;
        HealthChecksBuilder = Services.AddHealthChecks();

        return this;
    }
    public override IInfrastructureDependencyInjectionBuilder WithRuntimeConfiguration(
        IConfigurationBuilder configurationBuilder,
        in string folderPath)
    {
        configurationBuilder.AddJsonFile(folderPath, false, true);

        Services
            .AddOptions<MainConfiguration>()
            .Bind(Configuration.GetSection(MainConfiguration.ConfigurationSectionName))
            .ValidateOnStart()
            ;

        Services
            .AddSingleton<IValidateOptions<MainConfiguration>, OptionsValidators<MainConfiguration>>()
            .AddValidatorsFromAssemblies(new[]
            {
                //TODO: cleaner
                typeof(DependencyInjections).Assembly, typeof(Domain.DependencyInjection).Assembly
            });

        return this;
    }

    public override IInfrastructureDependencyInjectionBuilder WithSSMS()
    {
        services
            .AddMemoryCache();

        services
            .AddSingleton<IServerConnectionFactory, ServerConnectionFactory>()
            .AddSingleton<IServerPort, ServerAdapter>()
            .AddSingleton<IDatabasePort, DatabaseAdapter>()
            .AddSingleton<ITablePort, TableAdapter>()
            .AddSingleton<IViewPort, ViewAdapter>()
            .AddSingleton<IStoredProcedurePort, StoredProcedureAdapter>()
            .AddSingleton<IUserDefinedFunctionPort, UserDefinedFunctionAdapter>()
            .AddSingleton<IUserDefinedTypePort, UserDefinedTypeAdapter>()
            .AddSingleton<IUserDefinedTableTypePort, UserDefinedTableTypeAdapter>()
            .AddSingleton<IUserPort, UserAdapter>()
            .AddSingleton<ITriggerPort, TriggerAdapter>()
            .AddSingleton<IRolePort, RoleAdapter>()
            ;

        if (EnableHeathChecks)
        {
            MainConfiguration config = new();
            Configuration
                .GetSection(MainConfiguration.ConfigurationSectionName)
                .Bind(config);

            foreach (var dataSource in config.DataSources)
            {
                HealthChecksBuilder.AddSqlServer(dataSource.ConnectionString, name: $"sqlserver-{dataSource.Name}");
            }
        }

        return this;
    }
}
