using ssmsmcp.Domain.Abstractions.Configurations;

namespace ssmsmcp.Infrastructure.Configurations;

public sealed class MainConfiguration
    : IMainConfiguration
{
    public const string ConfigurationSectionName = "main";

    public DataSource[] DataSources { get; set; }
}

public sealed class DataSource
{
    public string Name { get; set; }
    public string ConnectionString { get; set; }
}
