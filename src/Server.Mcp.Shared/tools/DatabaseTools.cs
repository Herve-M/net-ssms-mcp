using System.ComponentModel;
using System.Text.Json;
using Mediator;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Databases;

namespace ssmsmcp.Server.Mcp.tools;

/// <summary>
/// MCP tools for SQL Server database information retrieval.
/// These tools delegate to Application layer Mediator handlers to fetch database metadata and configuration.
/// </summary>
internal sealed class DatabaseTools(IMediator mediator)
{
    private readonly IMediator _mediator = mediator;

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves a paginated list of schemas from a SQL Server database with their ID and name.")]
    public async Task<string> GetDatabaseSchemas(
        [Description("Name of the SQL Server instance")] string serverName,
        [Description("Name of the database")] string databaseName,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of items per page (max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        var request = new GetDatabaseSchemasRequest(serverName, databaseName, pagination);
        PagedResult<DatabaseSchemaDto> result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}
