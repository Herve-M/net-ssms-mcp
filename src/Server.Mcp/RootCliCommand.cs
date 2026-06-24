using DotMake.CommandLine;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Server.Mcp.Cli;

namespace ssmsmcp.Server.Mcp;

[CliCommand(Description = "SSMS MCP server — exposes a single SQL Server target over stdio MCP.")]
public sealed class RootCliCommand
{
    // Required = false on both: the two options are mutually exclusive and exactly one
    // must be supplied. That rule is enforced in RunAsync, not by per-option requiredness.
    [CliOption(Name = "--config", Alias = "-c", Required = false,
        Description = "Path to a configuration file; only the first data-source is used (exposed as 'main').")]
    public string? Config { get; set; }

    [CliOption(Name = "--server", Alias = "-s", Required = false,
        Description = "SQL Server connection string; exposed as the single 'main' data-source.")]
    public string? Server { get; set; }

    public async Task<int> RunAsync()
    {
        bool hasConfig = !string.IsNullOrWhiteSpace(Config);
        bool hasServer = !string.IsNullOrWhiteSpace(Server);

        if (hasConfig == hasServer)
        {
            await Console.Error.WriteLineAsync(
                "Specify exactly one of -c|--config or -s|--server.");
            return 2;
        }

        MainConfiguration configuration;

        try
        {
            configuration = hasServer
                ? MainConfigurationFactory.FromServer(Server!)
                : MainConfigurationFactory.FromConfigFile(Config!);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            return 1;
        }

        await McpCliHost.RunAsync(configuration);
        return 0;
    }
}
