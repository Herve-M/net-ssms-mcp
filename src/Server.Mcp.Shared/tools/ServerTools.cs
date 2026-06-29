using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Servers;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class ServerTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IDefaultServerName _defaultServerName = defaultServerName;
    private readonly IMediator _mediator = mediator;

    [McpServerTool(
        Name = "get_server_info",
        Title = "Get Server Info",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns server-level information: edition, version, platform, feature availability, security, engine, identity, localization and availability.")]
    public async Task<CallToolResult> GetServerInfo(
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Bypass any metadata cache for this call.")]
        bool force_refresh = false,
        CancellationToken cancellationToken = default)
    {
        // MCP HTTP => multi server support, MCP stdio => single server support (hardcoded to 'main')
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        ServerInfoResult result = new()
        {
            Overview = await _mediator.Send(new GetServerOverviewRequest(resolved), cancellationToken),
            Version = await _mediator.Send(new GetServerVersionRequest(resolved), cancellationToken),
            Platform = await _mediator.Send(new GetServerPlatformRequest(resolved), cancellationToken),
            Features = await _mediator.Send(new GetServerFeaturesRequest(resolved), cancellationToken),
            Security = await _mediator.Send(new GetServerSecurityRequest(resolved), cancellationToken),
            Engine = await _mediator.Send(new GetServerEngineRequest(resolved), cancellationToken),
            Identity = await _mediator.Send(new GetServerIdentityRequest(resolved), cancellationToken),
            Localization = await _mediator.Send(new GetServerLocalizationRequest(resolved), cancellationToken),
            Availability = await _mediator.Send(new GetServerAvailabilityRequest(resolved), cancellationToken),
        };

        return ToolPayload.Structured(result);
    }
}
