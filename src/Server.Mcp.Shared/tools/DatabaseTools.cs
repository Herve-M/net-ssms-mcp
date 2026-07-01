using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Databases;
using ssmsmcp.Application.Servers;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class DatabaseTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

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
        bool include_system = false, //TODO
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        //TODO: not yet implemented — plumb a cache-bypass flag through GetServerDatabases.
        [Description("Reserved / not yet implemented: intended to bypass any metadata cache for this call.")]
        bool force_refresh = false,
        CancellationToken cancellationToken = default)
    {
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

    [McpServerTool(
        Name = "list_objects",
        Title = "List Objects",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists database objects (tables, views, procedures, functions, synonyms, sequences, types, XML schema collections, service queues) in a database. Supports schema, object-type, and case-insensitive name filtering plus pagination.")]
    public async Task<CallToolResult> ListObjects(
        [Description("Target database to enumerate objects from.")]
        string database,
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Restrict results to one schema (case-insensitive exact match). Null returns all schemas.")]
        string? schema = null,
        [Description("Object type filter: TABLE, VIEW, PROCEDURE, FUNCTION, SYNONYM, SEQUENCE, TYPE, XML_SCHEMA_COLLECTION, SERVICE_QUEUE, ANY. Null or ANY returns all supported types.")]
        string? object_type = null,
        [Description("Case-insensitive substring to filter object names. Null returns all.")]
        string? name_pattern = null,
        [Description("Include objects in system schemas (sys, INFORMATION_SCHEMA).")]
        bool include_system = false,
        [Description("Bypass the cached object list and re-enumerate for this call.")]
        bool force_refresh = false,
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        PageRequest pagination = new()
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(page_size, 1, 100),
        };

        PagedResult<DatabaseObjectDto> result = await _mediator.Send(
            new GetDatabaseObjectsRequest(resolved, database, object_type, schema, name_pattern, include_system, force_refresh, pagination),
            cancellationToken);

        return ToolPayload.Structured(result);
    }
}
