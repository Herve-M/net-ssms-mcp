using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Procedures;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class ProcedureTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

    [McpServerTool(
        Name = "describe_procedure",
        Title = "Describe Procedure",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Describes a stored procedure, scalar/table-valued function, or CLR aggregate: parameters, return shape, execution context, and optional body/first-result-set introspection.")]
    public async Task<CallToolResult> DescribeProcedure(
        [Description("Target database containing the object.")]
        string database,
        [Description("Procedure, function, or aggregate name.")]
        string name,
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Schema containing the object.")]
        string schema = "dbo",
        [Description("Include the T-SQL body text. Always null when the object is encrypted or is a CLR aggregate (no T-SQL body exists).")]
        bool include_body = true,
        [Description("Include first_result_set_columns via sys.dm_exec_describe_first_result_set_for_object. Only supported for kind=PROCEDURE (T-SQL); null with a warning for every other kind.")]
        bool include_first_result_set = false,
        [Description("Reserved / not yet implemented: intended to bypass any metadata cache for this call.")]
        bool force_refresh = false, //TODO
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        DescribeProcedureDto? result = await _mediator.Send(
            new DescribeProcedureRequest(resolved, database, schema, name, include_body, include_first_result_set),
            cancellationToken);

        if (result is null)
        {
            return ToolPayload.NotFound($"Procedure, function, or aggregate '{schema}.{name}' does not exist in database '{database}'.");
        }

        return ToolPayload.Structured(result);
    }
}
