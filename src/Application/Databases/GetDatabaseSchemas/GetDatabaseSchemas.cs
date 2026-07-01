using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

/// <summary>
/// Data transfer object representing a database schema summary.
/// </summary>
public sealed record DatabaseSchemaDto
{
    /// <summary>
    /// Gets the schema ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Request to retrieve a paginated list of database schemas.
/// </summary>
public sealed record GetDatabaseSchemasRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseSchemaDto>>;

/// <summary>
/// Handler for retrieving paginated database schemas.
/// </summary>
public sealed class GetDatabaseSchemasHandler(ILogger<GetDatabaseSchemasHandler> logger, IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseSchemasRequest, PagedResult<DatabaseSchemaDto>>
{
    private readonly ILogger<GetDatabaseSchemasHandler> _logger = logger;
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<PagedResult<DatabaseSchemaDto>> Handle(GetDatabaseSchemasRequest request, CancellationToken cancellationToken)
    {

        // Validate pagination parameters
        request.Pagination.Validate();

        // Get total count
        int totalCount = await _databasePort.GetDatabaseSchemasCount(request.ServerName, request.DatabaseName, cancellationToken);

        // Get paginated schemas
        IReadOnlyCollection<Schema> schemas = await _databasePort.GetDatabaseSchemas(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        // Map to DTOs
        DatabaseSchemaDto[] schemaDtos = schemas
            .Select(schema => new DatabaseSchemaDto
            {
                Id = schema.ID,
                Name = schema.Name
            })
            .ToArray();

        return PagedResult<DatabaseSchemaDto>.Create(
            schemaDtos,
            totalCount,
            request.Pagination.Page,
            request.Pagination.PageSize);
    }
}
