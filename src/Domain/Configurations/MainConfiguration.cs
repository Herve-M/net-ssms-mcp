using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ssmsmcp.Domain.Abstractions.Configurations;

namespace ssmsmcp.Domain.Configurations;

public sealed class MainConfiguration
    : IMainConfiguration
{
    [JsonIgnore]
    public const string ConfigurationSectionName = "main";

    [ConfigurationKeyName("data-source")]
    public DataSource[] DataSources { get; set; }
}

public sealed class DataSource
{
    public string Name { get; set; }

    public string ConnectionString { get; set; }
}
