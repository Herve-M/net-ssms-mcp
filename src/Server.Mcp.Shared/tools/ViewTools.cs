using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Views;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class ViewTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

    [McpServerTool(
        Name = "describe_view",
        Title = "Describe View",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Rich description of a single view: columns, indexes (for indexed views), triggers, optional statistics, definition text, schema-binding, and encryption status.")]
    public async Task<CallToolResult> DescribeView(
        [Description("Target database containing the view.")]
        string database,
        [Description("View name.")]
        string name,
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Schema containing the view.")]
        string schema = "dbo",
        [Description("Include the indexes[] array. Only non-empty for indexed views (has_index=true).")]
        bool include_indexes = true,
        [Description("Include the triggers[] array (INSTEAD OF triggers).")]
        bool include_triggers = true,
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

        DescribeViewDto? result = await _mediator.Send(
            new DescribeViewRequest(
                resolved,
                database,
                schema,
                name,
                include_indexes,
                include_triggers,
                include_statistics),
            cancellationToken);

        if (result is null)
        {
            return ToolPayload.NotFound($"View '{schema}.{name}' does not exist in database '{database}'.");
        }

        return ToolPayload.Structured(result);
    }
}
