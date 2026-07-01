using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Servers;
using ssmsmcp.Server.Api.Models.V1;

namespace ssmsmcp.Server.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ServersController(IMediator mediator)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServerListItemResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServers(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ServerListItemDto> result = await mediator.Send(new GetServersListRequest(), cancellationToken);
        ServerListItemResponse[] response = result
            .Select(server => new ServerListItemResponse
            {
                ServerName = server.ServerName,
                ServerVersion = server.ServerVersion,
                DatabaseEngineType = server.DatabaseEngineType,
                DatabaseEngineEdition = server.DatabaseEngineEdition
            })
            .ToArray();

        return Ok(response);
    }

    [HttpGet("{serverName}")]
    [ProducesResponseType(typeof(ServerOverviewResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerOverview(string serverName, CancellationToken cancellationToken)
    {
        ServerOverviewDto result = await mediator.Send(new GetServerOverviewRequest(serverName), cancellationToken);
        var response = new ServerOverviewResponse
        {
            ServerName = result.ServerName,
            InstanceName = result.InstanceName,
            Edition = result.Edition,
            EngineEdition = result.EngineEdition,
            ProductLevel = result.ProductLevel,
            ProductUpdateLevel = result.ProductUpdateLevel,
            VersionString = result.VersionString,
            Status = result.Status,
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/identity")]
    [ProducesResponseType(typeof(ServerIdentityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerIdentity(string serverName, CancellationToken cancellationToken)
    {
        ServerIdentityDto result = await mediator.Send(new GetServerIdentityRequest(serverName), cancellationToken);
        var response = new ServerIdentityResponse
        {
            ServerName = result.ServerName,
            InstanceName = result.InstanceName,
            NetName = result.NetName,
            ComputerNamePhysicalNetBIOS = result.ComputerNamePhysicalNetBIOS,
            ServiceName = result.ServiceName,
            ServiceInstanceId = result.ServiceInstanceId
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/version")]
    [ProducesResponseType(typeof(ServerVersionResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerVersion(string serverName, CancellationToken cancellationToken)
    {
        ServerVersionDto result = await mediator.Send(new GetServerVersionRequest(serverName), cancellationToken);
        var response = new ServerVersionResponse
        {
            BuildNumber = result.BuildNumber,
            VersionMajor = result.VersionMajor,
            VersionMinor = result.VersionMinor,
            VersionString = result.VersionString,
            Product = result.Product,
            ProductLevel = result.ProductLevel,
            ProductUpdateLevel = result.ProductUpdateLevel,
            ResourceVersionString = result.ResourceVersionString
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/engine")]
    [ProducesResponseType(typeof(ServerEngineResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerEngine(string serverName, CancellationToken cancellationToken)
    {
        ServerEngineDto result = await mediator.Send(new GetServerEngineRequest(serverName), cancellationToken);
        var response = new ServerEngineResponse
        {
            Edition = result.Edition,
            EngineEdition = result.EngineEdition,
            ServerType = result.ServerType,
            IsSingleUser = result.IsSingleUser,
            IsCaseSensitive = result.IsCaseSensitive,
            IsXTPSupported = result.IsXTPSupported
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/platform")]
    [ProducesResponseType(typeof(ServerPlatformResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerPlatform(string serverName, CancellationToken cancellationToken)
    {
        ServerPlatformDto result = await mediator.Send(new GetServerPlatformRequest(serverName), cancellationToken);
        var response = new ServerPlatformResponse
        {
            HostPlatform = result.HostPlatform,
            HostDistribution = result.HostDistribution,
            HostRelease = result.HostRelease,
            HostServicePackLevel = result.HostServicePackLevel,
            Platform = result.Platform,
            OSVersion = result.OSVersion
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/localization")]
    [ProducesResponseType(typeof(ServerLocalizationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerLocalization(string serverName, CancellationToken cancellationToken)
    {
        ServerLocalizationDto result = await mediator.Send(new GetServerLocalizationRequest(serverName), cancellationToken);
        var response = new ServerLocalizationResponse
        {
            Language = result.Language,
            CollationID = result.CollationID,
            ComparisonStyle = result.ComparisonStyle,
            SqlCharSetName = result.SqlCharSetName,
            SqlSortOrderName = result.SqlSortOrderName
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/storage")]
    [ProducesResponseType(typeof(ServerStorageResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerStorage(string serverName, CancellationToken cancellationToken)
    {
        ServerStorageDto result = await mediator.Send(new GetServerStorageRequest(serverName), cancellationToken);
        var response = new ServerStorageResponse
        {
            BackupDirectory = result.BackupDirectory,
            DefaultFile = result.DefaultFile,
            DefaultLog = result.DefaultLog,
            MasterDBPath = result.MasterDBPath,
            MasterDBLogPath = result.MasterDBLogPath,
            ErrorLogPath = result.ErrorLogPath
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/connectivity")]
    [ProducesResponseType(typeof(ServerConnectivityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerConnectivity(string serverName, CancellationToken cancellationToken)
    {
        ServerConnectivityDto result = await mediator.Send(new GetServerConnectivityRequest(serverName), cancellationToken);
        var response = new ServerConnectivityResponse
        {
            TcpEnabled = result.TcpEnabled,
            NamedPipesEnabled = result.NamedPipesEnabled,
            NetName = result.NetName
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/security")]
    [ProducesResponseType(typeof(ServerSecurityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerSecurity(string serverName, CancellationToken cancellationToken)
    {
        ServerSecurityDto result = await mediator.Send(new GetServerSecurityRequest(serverName), cancellationToken);
        var response = new ServerSecurityResponse
        {
            LoginMode = result.LoginMode,
            AuditLevel = result.AuditLevel,
            ServiceAccount = result.ServiceAccount,
            IsContainedAuthentication = result.IsContainedAuthentication,
            SqlDomainGroup = result.SqlDomainGroup
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/availability")]
    [ProducesResponseType(typeof(ServerAvailabilityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerAvailability(string serverName, CancellationToken cancellationToken)
    {
        ServerAvailabilityDto result = await mediator.Send(new GetServerAvailabilityRequest(serverName), cancellationToken);
        var response = new ServerAvailabilityResponse
        {
            IsHadrEnabled = result.IsHadrEnabled,
            HadrManagerStatus = result.HadrManagerStatus,
            IsClustered = result.IsClustered,
            ClusterName = result.ClusterName,
            ClusterQuorumType = result.ClusterQuorumType,
            ClusterQuorumState = result.ClusterQuorumState
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/features")]
    [ProducesResponseType(typeof(ServerFeaturesResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerFeatures(string serverName, CancellationToken cancellationToken)
    {
        ServerFeaturesDto result = await mediator.Send(new GetServerFeaturesRequest(serverName), cancellationToken);
        var response = new ServerFeaturesResponse
        {
            IsJsonDataTypeEnabled = result.IsJsonDataTypeEnabled,
            IsXTPSupported = result.IsXTPSupported,
            IsPolyBaseInstalled = result.IsPolyBaseInstalled,
            IsFullTextInstalled = result.IsFullTextInstalled,
            FilestreamLevel = result.FilestreamLevel,
            FilestreamShareName = result.FilestreamShareName
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/capacity")]
    [ProducesResponseType(typeof(ServerCapacityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerCapacity(string serverName, CancellationToken cancellationToken)
    {
        ServerCapacityDto result = await mediator.Send(new GetServerCapacityRequest(serverName), cancellationToken);
        var response = new ServerCapacityResponse
        {
            Processors = result.Processors,
            PhysicalMemory = result.PhysicalMemory,
            PhysicalMemoryUsageInKB = result.PhysicalMemoryUsageInKB,
            ReservedStorageSizeMB = result.ReservedStorageSizeMB,
            UsedStorageSizeMB = result.UsedStorageSizeMB,
            NumberOfLogFiles = result.NumberOfLogFiles
        };

        return Ok(response);
    }

    [HttpGet("{serverName}/databases")]
    [ProducesResponseType(typeof(PagedResult<ServerDatabaseListItemResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetServerDatabases(
        string serverName,
        [FromQuery] string? name_pattern = null,
        [FromQuery] int page = 1,
        [FromQuery] int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        PageRequest pagination = new() { Page = Math.Max(page, 1), PageSize = Math.Clamp(page_size, 1, 100) };
        PagedResult<ServerDatabaseListItemDto> result = await mediator.Send(
            new GetServerDatabasesRequest(serverName, name_pattern, false, pagination), cancellationToken);

        PagedResult<ServerDatabaseListItemResponse> response = PagedResult<ServerDatabaseListItemResponse>.Create(
            result.Items.Select(d => new ServerDatabaseListItemResponse { Id = d.Id, DatabaseName = d.DatabaseName }).ToArray(),
            result.TotalCount, result.Page, result.PageSize);

        return Ok(response);
    }
}
