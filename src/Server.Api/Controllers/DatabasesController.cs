using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Databases;
using ssmsmcp.Server.Api.Models.V1;

namespace ssmsmcp.Server.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/Servers/{serverName}/databases")]
public sealed class DatabasesController(IMediator mediator)
    : ControllerBase
{
    [HttpGet("{databaseName}")]
    [ProducesResponseType(typeof(DatabaseDetailsResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseDetails(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseDetailsDto result = await mediator.Send(new GetDatabaseDetailsRequest(serverName, databaseName), cancellationToken);
        var response = new DatabaseDetailsResponse
        {
            DatabaseName = result.DatabaseName,
            Id = result.Id,
            DatabaseGuid = result.DatabaseGuid,
            Status = result.Status,
            IsAccessible = result.IsAccessible,
            IsSystemObject = result.IsSystemObject,
            IsDatabaseSnapshot = result.IsDatabaseSnapshot,
            IsFabricDatabase = result.IsFabricDatabase,
            CreateDate = result.CreateDate
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/configuration")]
    [ProducesResponseType(typeof(DatabaseConfigurationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseConfiguration(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseConfigurationDto result = await mediator.Send(new GetDatabaseConfigurationRequest(serverName, databaseName), cancellationToken);
        var response = new DatabaseConfigurationResponse
        {
            Collation = result.Collation,
            CompatibilityLevel = result.CompatibilityLevel,
            ContainmentType = result.ContainmentType,
            RecoveryModel = result.RecoveryModel,
            AnsiNullDefault = result.AnsiNullDefault,
            AnsiNullsEnabled = result.AnsiNullsEnabled,
            AnsiPaddingEnabled = result.AnsiPaddingEnabled,
            AnsiWarningsEnabled = result.AnsiWarningsEnabled,
            ArithmeticAbortEnabled = result.ArithmeticAbortEnabled,
            AutoClose = result.AutoClose,
            AutoShrink = result.AutoShrink,
            AutoUpdateStatisticsEnabled = result.AutoUpdateStatisticsEnabled,
            AutoUpdateStatisticsAsync = result.AutoUpdateStatisticsAsync,
            AutoCreateStatisticsEnabled = result.AutoCreateStatisticsEnabled,
            AutoCreateIncrementalStatisticsEnabled = result.AutoCreateIncrementalStatisticsEnabled,
            IsParameterizationForced = result.IsParameterizationForced,
            RecursiveTriggersEnabled = result.RecursiveTriggersEnabled,
            NestedTriggersEnabled = result.NestedTriggersEnabled,
            PageVerify = result.PageVerify,
            QuotedIdentifiersEnabled = result.QuotedIdentifiersEnabled,
            NumericRoundAbortEnabled = result.NumericRoundAbortEnabled
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/storage")]
    [ProducesResponseType(typeof(DatabaseStorageResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseStorage(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseStorageDto result = await mediator.Send(new GetDatabaseStorageRequest(serverName, databaseName), cancellationToken);
        var response = new DatabaseStorageResponse
        {
            Size = result.Size,
            SpaceAvailable = result.SpaceAvailable,
            MaxSizeInBytes = result.MaxSizeInBytes,
            DataSpaceUsage = result.DataSpaceUsage,
            IndexSpaceUsage = result.IndexSpaceUsage,
            LastBackupDate = result.LastBackupDate,
            LastDifferentialBackupDate = result.LastDifferentialBackupDate,
            LastLogBackupDate = result.LastLogBackupDate,
            LastGoodCheckDbTime = result.LastGoodCheckDbTime,
            DefaultFileGroup = result.DefaultFileGroup,
            PrimaryFilePath = result.PrimaryFilePath,
            DefaultFullTextCatalog = result.DefaultFullTextCatalog
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/security")]
    [ProducesResponseType(typeof(DatabaseSecurityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseSecurity(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseSecurityDto result = await mediator.Send(new GetDatabaseSecurityRequest(serverName, databaseName), cancellationToken);
        var response = new DatabaseSecurityResponse
        {
            Owner = result.Owner,
            UserAccess = result.UserAccess,
            Trustworthy = result.Trustworthy,
            DatabaseOwnershipChaining = result.DatabaseOwnershipChaining,
            EncryptionEnabled = result.EncryptionEnabled,
            HasDatabaseEncryptionKey = result.HasDatabaseEncryptionKey,
            DboLogin = result.DboLogin,
            IsDbOwner = result.IsDbOwner,
            IsDbSecurityAdmin = result.IsDbSecurityAdmin,
            IsDbAccessAdmin = result.IsDbAccessAdmin,
            IsDbDatareader = result.IsDbDatareader,
            IsDbDatawriter = result.IsDbDatawriter,
            IsDbDdlAdmin = result.IsDbDdlAdmin
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/availability")]
    [ProducesResponseType(typeof(DatabaseAvailabilityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseAvailability(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseAvailabilityDto result = await mediator.Send(new GetDatabaseAvailabilityRequest(serverName, databaseName), cancellationToken);
        var response = new DatabaseAvailabilityResponse
        {
            AvailabilityGroupName = result.AvailabilityGroupName,
            AvailabilityDatabaseSynchronizationState = result.AvailabilityDatabaseSynchronizationState,
            IsMirroringEnabled = result.IsMirroringEnabled,
            MirroringStatus = result.MirroringStatus,
            MirroringPartner = result.MirroringPartner,
            ServiceBrokerGuid = result.ServiceBrokerGuid,
            BrokerEnabled = result.BrokerEnabled,
            ReplicationOptions = result.ReplicationOptions,
            LogReuseWaitStatus = result.LogReuseWaitStatus
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/features")]
    [ProducesResponseType(typeof(DatabaseFeaturesResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseFeatures(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        DatabaseFeaturesDto result = await mediator.Send(new GetDatabaseFeaturesRequest(serverName, databaseName), cancellationToken);
        DatabaseFeaturesResponse response = result switch
        {
            StandaloneDatabaseFeaturesDto standalone => new StandaloneDatabaseFeaturesResponse
            {
                IsFullTextEnabled = standalone.IsFullTextEnabled,
                ChangeTrackingEnabled = standalone.ChangeTrackingEnabled,
                ChangeTrackingAutoCleanUp = standalone.ChangeTrackingAutoCleanUp,
                ChangeTrackingRetentionPeriod = standalone.ChangeTrackingRetentionPeriod,
                ChangeTrackingRetentionPeriodUnits = standalone.ChangeTrackingRetentionPeriodUnits,
                IsLedger = standalone.IsLedger,
                HasMemoryOptimizedObjects = standalone.HasMemoryOptimizedObjects,
                MemoryAllocatedToMemoryOptimizedObjectsInKB = standalone.MemoryAllocatedToMemoryOptimizedObjectsInKB,
                MemoryUsedByMemoryOptimizedObjectsInKB = standalone.MemoryUsedByMemoryOptimizedObjectsInKB,
                FilestreamDirectoryName = standalone.FilestreamDirectoryName,
                FilestreamNonTransactedAccess = standalone.FilestreamNonTransactedAccess
            },
            SqlAzureDatabaseFeaturesDto sqlAzure => new SqlAzureDatabaseFeaturesResponse
            {
                IsFullTextEnabled = sqlAzure.IsFullTextEnabled,
                ChangeTrackingEnabled = sqlAzure.ChangeTrackingEnabled,
                ChangeTrackingAutoCleanUp = sqlAzure.ChangeTrackingAutoCleanUp,
                ChangeTrackingRetentionPeriod = sqlAzure.ChangeTrackingRetentionPeriod,
                ChangeTrackingRetentionPeriodUnits = sqlAzure.ChangeTrackingRetentionPeriodUnits,
                IsLedger = sqlAzure.IsLedger,
                TemporalHistoryRetentionEnabled = sqlAzure.TemporalHistoryRetentionEnabled,
                HasMemoryOptimizedObjects = sqlAzure.HasMemoryOptimizedObjects,
                MemoryAllocatedToMemoryOptimizedObjectsInKB = sqlAzure.MemoryAllocatedToMemoryOptimizedObjectsInKB,
                MemoryUsedByMemoryOptimizedObjectsInKB = sqlAzure.MemoryUsedByMemoryOptimizedObjectsInKB,
                IsSqlDw = sqlAzure.IsSqlDw,
                IsSqlDwEdition = sqlAzure.IsSqlDwEdition
            },
            _ => throw new InvalidOperationException($"Unsupported database features type '{result.GetType().Name}'.")
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/schemas")]
    [ProducesResponseType(typeof(PagedResult<DatabaseSchemaResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseSchemas(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseSchemaDto> result = await mediator.Send(
            new GetDatabaseSchemasRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseSchemaResponse>
        {
            Items = result.Items
                .Select(schema => new DatabaseSchemaResponse
                {
                    Id = schema.Id,
                    Name = schema.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/tables")]
    [ProducesResponseType(typeof(PagedResult<DatabaseTableResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseTables(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseTableDto> result = await mediator.Send(
            new GetDatabaseTablesRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseTableResponse>
        {
            Items = result.Items
                .Select(table => new DatabaseTableResponse
                {
                    Name = table.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/views")]
    [ProducesResponseType(typeof(PagedResult<DatabaseViewResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseViews(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseViewDto> result = await mediator.Send(
            new GetDatabaseViewsRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseViewResponse>
        {
            Items = result.Items
                .Select(view => new DatabaseViewResponse
                {
                    Name = view.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/stored-procedures")]
    [ProducesResponseType(typeof(PagedResult<DatabaseStoredProcedureResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseStoredProcedures(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseStoredProcedureDto> result = await mediator.Send(
            new GetDatabaseStoredProceduresRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseStoredProcedureResponse>
        {
            Items = result.Items
                .Select(storedProcedure => new DatabaseStoredProcedureResponse
                {
                    Name = storedProcedure.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/user-defined-functions")]
    [ProducesResponseType(typeof(PagedResult<DatabaseUserDefinedFunctionResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseUserDefinedFunctions(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseUserDefinedFunctionDto> result = await mediator.Send(
            new GetDatabaseUserDefinedFunctionsRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseUserDefinedFunctionResponse>
        {
            Items = result.Items
                .Select(userDefinedFunction => new DatabaseUserDefinedFunctionResponse
                {
                    Name = userDefinedFunction.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/user-defined-types")]
    [ProducesResponseType(typeof(PagedResult<DatabaseUserDefinedTypeResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseUserDefinedTypes(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseUserDefinedTypeDto> result = await mediator.Send(
            new GetDatabaseUserDefinedTypesRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseUserDefinedTypeResponse>
        {
            Items = result.Items
                .Select(userDefinedType => new DatabaseUserDefinedTypeResponse
                {
                    Name = userDefinedType.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/user-defined-table-types")]
    [ProducesResponseType(typeof(PagedResult<DatabaseUserDefinedTableTypeResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseUserDefinedTableTypes(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseUserDefinedTableTypeDto> result = await mediator.Send(
            new GetDatabaseUserDefinedTableTypesRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseUserDefinedTableTypeResponse>
        {
            Items = result.Items
                .Select(userDefinedTableType => new DatabaseUserDefinedTableTypeResponse
                {
                    Name = userDefinedTableType.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/users")]
    [ProducesResponseType(typeof(PagedResult<DatabaseUserResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseUsers(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseUserDto> result = await mediator.Send(
            new GetDatabaseUsersRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseUserResponse>
        {
            Items = result.Items
                .Select(user => new DatabaseUserResponse
                {
                    Name = user.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/triggers")]
    [ProducesResponseType(typeof(PagedResult<DatabaseTriggerResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseTriggers(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseTriggerDto> result = await mediator.Send(
            new GetDatabaseTriggersRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseTriggerResponse>
        {
            Items = result.Items
                .Select(trigger => new DatabaseTriggerResponse
                {
                    Name = trigger.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{databaseName}/roles")]
    [ProducesResponseType(typeof(PagedResult<DatabaseRoleResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetDatabaseRoles(
        string serverName,
        string databaseName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PageRequest
        {
            Page = page,
            PageSize = pageSize
        };

        PagedResult<DatabaseRoleDto> result = await mediator.Send(
            new GetDatabaseRolesRequest(serverName, databaseName, pagination),
            cancellationToken);

        var response = new PagedResult<DatabaseRoleResponse>
        {
            Items = result.Items
                .Select(role => new DatabaseRoleResponse
                {
                    Name = role.Name
                })
                .ToArray(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(response);
    }
}
