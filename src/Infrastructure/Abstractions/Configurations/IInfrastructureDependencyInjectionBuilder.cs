using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ssmsmcp.Domain.Configurations;

namespace ssmsmcp.Infrastructure.Abstractions.Configurations;

public interface IInfrastructureDependencyInjectionBuilder
{
    IServiceCollection Build();

    IInfrastructureDependencyInjectionBuilder WithFeatureToggle();
    IInfrastructureDependencyInjectionBuilder WithFileConfiguration(IConfigurationBuilder configurationBuilder, in string folderPath);
    IInfrastructureDependencyInjectionBuilder WithRuntimeConfiguration(IConfigurationBuilder configurationBuilder, MainConfiguration configuration);
    IInfrastructureDependencyInjectionBuilder WithSSMS();
    IInfrastructureDependencyInjectionBuilder WithHealthChecks();
    IInfrastructureDependencyInjectionBuilder WithOpenTelemetry(Action<OpenTelemetrySettings>? configure = null);
    IInfrastructureDependencyInjectionBuilder WithServiceDiscovery();
}
