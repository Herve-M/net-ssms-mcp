using Microsoft.Extensions.Configuration;
using ssmsmcp.Domain.Configurations;

namespace ssmsmcp.Server.Mcp.Cli;

public static class MainConfigurationFactory
{
    public const string MainDataSourceName = "main";

    public static MainConfiguration FromServer(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new MainConfiguration
        {
            DataSources =
            [
                new DataSource
                {
                    Name = MainDataSourceName,
                    ConnectionString = connectionString
                }
            ]
        };
    }

    public static MainConfiguration FromConfigFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: '{path}'.", path);
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(path, optional: false, reloadOnChange: false)
            .Build();

        MainConfiguration? loaded = configuration
            .GetSection(MainConfiguration.ConfigurationSectionName)
            .Get<MainConfiguration>();

        if (loaded?.DataSources is null || loaded.DataSources.Length == 0)
        {
            throw new InvalidOperationException(
                $"Configuration file '{path}' contains no '{MainConfiguration.ConfigurationSectionName}:data-source' entries.");
        }

        DataSource first = loaded.DataSources[0];

        return new MainConfiguration
        {
            DataSources =
            [
                new DataSource
                {
                    Name = MainDataSourceName,
                    ConnectionString = first.ConnectionString
                }
            ]
        };
    }
}
