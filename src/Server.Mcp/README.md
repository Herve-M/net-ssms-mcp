# MCP Server

## Checklist before publishing to NuGet.org

- Test the MCP server locally using the steps below.
- Update the package metadata in the .csproj file, in particular the `<PackageId>`.
- Update `.mcp/server.json` to declare your MCP server's inputs.
  - See [configuring inputs](https://aka.ms/nuget/mcp/guide/configuring-inputs) for more details.
- Pack the project using `dotnet pack`.

The `bin/Release` directory will contain the package file (.nupkg), which can be [published to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package).

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.

```json
{
  "servers": {
    "Server.Mcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
       "run",
        "--project",
        "src/Server.Mcp/Server.Mcp.csproj",
        "--",
        "-s",
        "<YOUR SQL SERVER CONNECTION STRING>"
      ]
    }
  }
}
```

> The server requires exactly one of `-s | --server` (a SQL Server connection string) or
> `-c | --config` (path to a JSON config file; only its first `data-source` is used). Both modes
> expose the target as a single data-source named `main`. To use a config file instead, replace
> `-s <CONNECTION STRING>` with `-c <PATH TO CONFIG FILE>`.

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)

## Using the MCP Server from NuGet.org

Once the MCP server package is published to NuGet.org, you can configure it in your preferred IDE. Both VS Code and Visual Studio use the `dnx` command to download and install the MCP server package from NuGet.org.

- **VS Code**: Create a `<WORKSPACE DIRECTORY>/.vscode/mcp.json` file
- **Visual Studio**: Create a `<SOLUTION DIRECTORY>\.mcp.json` file

For both VS Code and Visual Studio, the configuration file uses the following server definition:

```json
{
  "servers": {
    "Server.Mcp": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "ssms-mcp",
        "--version",
        "0.1.0-dev",
        "--yes",
        "-s",
        "<YOUR SQL SERVER CONNECTION STRING>"
      ]
    }
  }
}
```

> Pass `-c <PATH TO CONFIG FILE>` instead of `-s <CONNECTION STRING>` to load the first
> `data-source` from a JSON config file. Exactly one of the two is required.