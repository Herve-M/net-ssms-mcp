using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ssmsmcp.Infrastructure.Abstractions.Configurations;

public abstract class InfrastructureDependencyInjectionBuilder(
    IConfiguration configuration,
    IHostEnvironment environment,
    IServiceCollection services)
    : IInfrastructureDependencyInjectionBuilder
{
    protected internal IServiceCollection Services = services;
    protected internal readonly IConfiguration Configuration = configuration;
    protected internal readonly IHostEnvironment Environment = environment;

    protected internal IHealthChecksBuilder HealthChecksBuilder { get; protected set; }
    protected bool EnableHeathChecks { get; set; }
    public IServiceCollection Build() => Services;
    public abstract IInfrastructureDependencyInjectionBuilder WithFeatureToggle();
    public abstract IInfrastructureDependencyInjectionBuilder WithRuntimeConfiguration(IConfigurationBuilder configurationBuilder, in string folderPath);
    public abstract IInfrastructureDependencyInjectionBuilder WithSSMS();
    public abstract IInfrastructureDependencyInjectionBuilder WithHealthChecks();
    public abstract IInfrastructureDependencyInjectionBuilder WithOpenTelemetry(Action<OpenTelemetrySettings>? configure = null);
    public abstract IInfrastructureDependencyInjectionBuilder WithServiceDiscovery();
}
