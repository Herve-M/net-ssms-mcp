using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Databases;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class TableTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

    [McpServerTool(
        Name = "describe_table",
        Title = "Describe Table",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Rich description of a single table: columns, primary key, indexes, foreign keys (inbound and outbound), check constraints, triggers, and optional statistics.")]
    public async Task<CallToolResult> DescribeTable(
        [Description("Target database containing the table.")]
        string database,
        [Description("Table name.")]
        string name,
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Schema containing the table.")]
        string schema = "dbo",
        [Description("Include the indexes[] array. primary_key is always populated regardless of this flag.")]
        bool include_indexes = true,
        [Description("Include the foreign_keys_outbound[] and foreign_keys_inbound[] arrays.")]
        bool include_foreign_keys = true,
        [Description("Include the triggers[] array.")]
        bool include_triggers = true,
        [Description("Include the check_constraints[] array.")]
        bool include_check_constraints = true,
        [Description("Include the statistics[] array. last_updated and rows_sampled are always null (not retrievable without a DMV query).")]
        bool include_statistics = false,
        [Description("Reserved / not yet implemented: intended to bypass any metadata cache for this call.")]
        bool force_refresh = false, //TODO
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        DescribeTableDto? result = await _mediator.Send(
            new DescribeTableRequest(
                resolved,
                database,
                schema,
                name,
                include_indexes,
                include_foreign_keys,
                include_triggers,
                include_check_constraints,
                include_statistics),
            cancellationToken);

        if (result is null)
        {
            return ToolPayload.NotFound($"Table '{schema}.{name}' does not exist in database '{database}'.");
        }

        return ToolPayload.Structured(result);
    }
}
