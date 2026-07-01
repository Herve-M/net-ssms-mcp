using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseObjectDto
{
    public required string Database { get; init; }
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required string TypeDesc { get; init; }
    public required string Fqn { get; init; }

    // TODO: object_id is not exposed by Database.EnumObjects?
    public int ObjectId { get; init; }

    public DateTimeOffset? CreateDate { get; init; }
    public DateTimeOffset? ModifyDate { get; init; }
    public long? RowCount { get; init; }
    public double? SizeKb { get; init; }
}

public sealed record GetDatabaseObjectsRequest(
    string ServerName,
    string DatabaseName,
    string? ObjectType,
    string? Schema,
    string? NamePattern,
    bool IncludeSystem,
    bool ForceRefresh,
    PageRequest Pagination)
    : IRequest<PagedResult<DatabaseObjectDto>>;

public sealed class GetDatabaseObjectsHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseObjectsRequest, PagedResult<DatabaseObjectDto>>
{
    private readonly IDatabasePort _databasePort = databasePort;

    private static readonly string[] SystemSchemas = ["sys", "INFORMATION_SCHEMA"];

    public async ValueTask<PagedResult<DatabaseObjectDto>> Handle(
        GetDatabaseObjectsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        IReadOnlyCollection<DatabaseObjectInfo> objects =
            await _databasePort.GetDatabaseObjects(request.ServerName, request.DatabaseName, SupportedTypes, request.ForceRefresh, cancellationToken);

        IEnumerable<DatabaseObjectInfo> query = objects;

        string[]? typeFilter = MapTypeFilter(request.ObjectType);
        if (typeFilter is not null)
        {
            query = query.Where(o => typeFilter.Contains(o.Type, StringComparer.OrdinalIgnoreCase));
        }

        if (!request.IncludeSystem)
        {
            query = query.Where(o => !SystemSchemas.Contains(o.Schema, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Schema))
        {
            query = query.Where(o => string.Equals(o.Schema, request.Schema, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.NamePattern))
        {
            //TODO: intro minimatch/nanomatch style pattern matching for name_pattern?
            query = query.Where(o => o.Name.Contains(request.NamePattern, StringComparison.OrdinalIgnoreCase));
        }

        DatabaseObjectInfo[] filtered = query
            .OrderBy(o => o.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        DatabaseObjectDto[] pageItems = filtered
            .Skip(request.Pagination.Skip)
            .Take(request.Pagination.Take)
            .Select(o => new DatabaseObjectDto
            {
                Database = request.DatabaseName,
                Schema = o.Schema,
                Name = o.Name,
                TypeDesc = MapTypeDesc(o.Type),
                Fqn = BuildFqn(request.DatabaseName, o.Schema, o.Name),
            })
            .ToArray();

        return PagedResult<DatabaseObjectDto>.Create(
            pageItems, filtered.Length, request.Pagination.Page, request.Pagination.PageSize);
    }

    private const DatabaseObjectTypes SupportedTypes =
        DatabaseObjectTypes.Table
        | DatabaseObjectTypes.View
        | DatabaseObjectTypes.StoredProcedure
        | DatabaseObjectTypes.UserDefinedFunction
        | DatabaseObjectTypes.Synonym
        | DatabaseObjectTypes.Sequence
        | DatabaseObjectTypes.UserDefinedType
        | DatabaseObjectTypes.UserDefinedDataType
        | DatabaseObjectTypes.UserDefinedTableTypes
        | DatabaseObjectTypes.XmlSchemaCollection
        | DatabaseObjectTypes.ServiceQueue;

    private static string[]? MapTypeFilter(string? objectType) => objectType?.ToUpperInvariant() switch
    {
        null or "ANY" => null,
        "TABLE" => ["Table"],
        "VIEW" => ["View"],
        "PROCEDURE" => ["StoredProcedure"],
        "FUNCTION" => ["UserDefinedFunction"],
        "SYNONYM" => ["Synonym"],
        "SEQUENCE" => ["Sequence"],
        "TYPE" => ["UserDefinedType", "UserDefinedDataType", "UserDefinedTableTypes"],
        "XML_SCHEMA_COLLECTION" => ["XmlSchemaCollection"],
        "SERVICE_QUEUE" => ["ServiceQueue"],
        _ => null,
    };

    //TOSEE: move to extension/helper?
    private static string MapTypeDesc(string smoType) => smoType switch
    {
        "Table" => "USER_TABLE",
        "View" => "VIEW",
        "StoredProcedure" => "SQL_STORED_PROCEDURE",
        "UserDefinedFunction" => "SQL_FUNCTION",
        "Synonym" => "SYNONYM",
        "Sequence" => "SEQUENCE_OBJECT",
        "UserDefinedType" or "UserDefinedDataType" => "USER_DEFINED_TYPE",
        "UserDefinedTableType" or "UserDefinedTableTypes" => "USER_DEFINED_TABLE_TYPE",
        "XmlSchemaCollection" => "XML_SCHEMA_COLLECTION",
        "ServiceQueue" => "SERVICE_QUEUE",
        _ => smoType.ToUpperInvariant(),
    };

    private static string BuildFqn(string database, string schema, string name) =>
        $"{Quote(database)}.{Quote(schema)}.{Quote(name)}";

    //TODO: move to a helper?
    private static string Quote(string identifier) =>
        IsSimpleIdentifier(identifier) ? identifier : $"[{identifier.Replace("]", "]]")}]";

    //TODO: move to a helper?
    // escape identifiers with SQL validity
    private static bool IsSimpleIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
