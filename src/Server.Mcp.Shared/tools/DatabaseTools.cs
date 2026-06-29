using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Servers;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class DatabaseTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

    //TODO: use paging but application layer dooesn´t support it
    [McpServerTool(
        Name = "list_databases",
        Title = "List Databases",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists databases on the target SQL Server instance. Supports case-insensitive name filtering and pagination.")]
    public async Task<CallToolResult> ListDatabases(
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Case-insensitive substring to filter database names. Null returns all.")]
        string? name_pattern = null,
        [Description("Include system databases (master/tempdb/model/msdb). Not yet applied; reserved.")]
        bool include_system = false,
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        [Description("Bypass any metadata cache for this call.")]
        bool force_refresh = false,
        CancellationToken cancellationToken = default)
    {
        // MCP HTTP => multi server support, MCP stdio => single server support (hardcoded to 'main')
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        IReadOnlyCollection<ServerDatabaseListItemDto> databases =
            await _mediator.Send(new GetServerDatabasesRequest(resolved), cancellationToken);

        IEnumerable<ServerDatabaseListItemDto> query = databases;

        if (!string.IsNullOrWhiteSpace(name_pattern))
        {
            query = query.Where(d => d.DatabaseName.Contains(name_pattern, StringComparison.OrdinalIgnoreCase));
        }

        ServerDatabaseListItemDto[] ordered = query
            .OrderBy(d => d.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        PageRequest pagination = new()
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(page_size, 1, 100),
        };

        ServerDatabaseListItemDto[] pageItems = ordered
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToArray();

        PagedResult<ServerDatabaseListItemDto> result =
            PagedResult<ServerDatabaseListItemDto>.Create(pageItems, ordered.Length, pagination.Page, pagination.PageSize);

        return ToolPayload.Structured(result);
    }
}
