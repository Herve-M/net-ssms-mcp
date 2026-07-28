# Server.Mcp — AGENTS.md

The stdio MCP host: a CLI executable that serves **one** SQL Server target over JSON-RPC on
stdin/stdout. Composes `Infrastructure` + `Application` + `Server.Mcp.Shared`. The tools
themselves live in `Server.Mcp.Shared` — this project only wires and hosts them.

Tool-authoring and transport conventions: [`mcp.md`](../../.claude/rules/mcp.md).

## Startup path

`Program.cs` → `Cli.RunAsync<RootCliCommand>(args)` (DotMake.CommandLine) →
`RootCliCommand.RunAsync()` → `MainConfigurationFactory` → `McpCliHost.RunAsync(configuration)`.

## Boundaries

- **`Program.cs`** — one line; hands off to the CLI framework.
- **`RootCliCommand.cs`** — the `[CliCommand]` surface. Two mutually complementary options:
  `--server` / `-s` (a raw connection string) and `--config` / `-c` (a config file). Builds
  the `MainConfiguration` and calls the host.
- **`Cli/MainConfigurationFactory.cs`** — turns those options into the single registered
  data-source. `MainDataSourceName` (`"main"`) is the name every tool resolves to when the
  caller omits `server_name`; it is the contract between this host and `ServerNameResolver`.
- **`Cli/McpCliHost.cs`** — the composition root. In order: **stderr logging**
  (`LogToStandardErrorThreshold = LogLevel.Trace` — stdout is the JSON-RPC channel, writing
  to it corrupts the protocol), `UseInfrastructureLayer(...)` with the fluent `With…()` chain,
  `UseApplicationLayer(...)`, `AddDefaultServerName(MainConfigurationFactory.MainDataSourceName)`,
  `AddMediator(...)`, then `AddMcpServer(...).WithStdioServerTransport().AddTools()`.
- **`.mcp/`** — client registration samples.

## Notes

- The server version reported in `ServerInfo` comes from
  `AssemblyInformationalVersionAttribute`, falling back to `"0.1.0-beta"` in debug builds.
- This project is deliberately **absent** from the Aspire host — a stdio server is spawned by
  its client, not orchestrated. See `Server.Aspire.Host/AGENTS.md`.
- `ModelContextProtocol.AspNetCore` must never be referenced here; it belongs to `Server.Api`.
