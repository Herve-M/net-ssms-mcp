using System.ComponentModel;
using System.Text.Json;
using Mediator;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Servers;

namespace ssmsmcp.Server.Mcp.tools;

/// <summary>
/// MCP tools for SQL Server information retrieval.
/// These tools delegate to Application layer Mediator handlers to fetch server metadata and configuration.
/// </summary>
internal sealed class ServerTools(IMediator mediator)
{
    private readonly IMediator _mediator = mediator;

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves a list of all available SQL Server instances with basic information.")]
    public async Task<string> GetServersList(CancellationToken cancellationToken)
    {
        var request = new GetServersListRequest();
        IReadOnlyCollection<ServerListItemDto> result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves detailed version and build information for a SQL Server instance.")]
    public async Task<string> GetServerVersion(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerVersionRequest(serverName);
        ServerVersionDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves a list of all databases on a SQL Server instance.")]
    public async Task<string> GetServerDatabases(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerDatabasesRequest(serverName);
        IReadOnlyCollection<ServerDatabaseListItemDto> result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves availability information including HADR and clustering configuration for a SQL Server instance.")]
    public async Task<string> GetServerAvailability(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerAvailabilityRequest(serverName);
        ServerAvailabilityDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves capacity and resource information including CPU, memory, and storage metrics for a SQL Server instance.")]
    public async Task<string> GetServerCapacity(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerCapacityRequest(serverName);
        ServerCapacityDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves network connectivity settings including enabled protocols for a SQL Server instance.")]
    public async Task<string> GetServerConnectivity(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerConnectivityRequest(serverName);
        ServerConnectivityDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves engine edition, type, and capability information for a SQL Server instance.")]
    public async Task<string> GetServerEngine(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerEngineRequest(serverName);
        ServerEngineDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves information about installed and enabled features for a SQL Server instance.")]
    public async Task<string> GetServerFeatures(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerFeaturesRequest(serverName);
        ServerFeaturesDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves identity and naming information for a SQL Server instance.")]
    public async Task<string> GetServerIdentity(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerIdentityRequest(serverName);
        ServerIdentityDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves localization settings including language and collation information for a SQL Server instance.")]
    public async Task<string> GetServerLocalization(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerLocalizationRequest(serverName);
        ServerLocalizationDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves a comprehensive overview of a SQL Server instance including edition, version, and status.")]
    public async Task<string> GetServerOverview(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerOverviewRequest(serverName);
        ServerOverviewDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves platform and operating system information for a SQL Server instance.")]
    public async Task<string> GetServerPlatform(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerPlatformRequest(serverName);
        ServerPlatformDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves security configuration and authentication settings for a SQL Server instance.")]
    public async Task<string> GetServerSecurity(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerSecurityRequest(serverName);
        ServerSecurityDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Destructive = false, Idempotent = true, ReadOnly = true)]
    [Description("Retrieves storage path and directory information for a SQL Server instance.")]
    public async Task<string> GetServerStorage(
        [Description("Name of the SQL Server instance")] string serverName,
        CancellationToken cancellationToken)
    {
        var request = new GetServerStorageRequest(serverName);
        ServerStorageDto result = await _mediator.Send(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}
