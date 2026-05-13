using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ssmsmcp.Infrastructure.Abstractions.Configurations;

public interface IInfrastructureDependencyInjectionBuilder
{
    IServiceCollection Build();

    IInfrastructureDependencyInjectionBuilder WithFeatureToggle();
    IInfrastructureDependencyInjectionBuilder WithRuntimeConfiguration(IConfigurationBuilder configurationBuilder, in string folderPath);
    IInfrastructureDependencyInjectionBuilder WithSSMS();
    IInfrastructureDependencyInjectionBuilder WithHealthChecks();
    IInfrastructureDependencyInjectionBuilder WithOpenTelemetry(Action<OpenTelemetrySettings>? configure = null);
    IInfrastructureDependencyInjectionBuilder WithServiceDiscovery();
}
