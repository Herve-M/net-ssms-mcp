using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record ColumnDto
{
    public required string Name { get; init; }
    public required int Ordinal { get; init; }
    public required string DataType { get; init; }
    public required bool IsNullable { get; init; }
    public required bool IsIdentity { get; init; }
    public required bool IsComputed { get; init; }
    public string? ComputedDefinition { get; init; }
    public bool? IsPersisted { get; init; }
    public string? DefaultConstraint { get; init; }
    public string? Collation { get; init; }
    public required bool IsMasked { get; init; }
    public string? MaskFunction { get; init; }
}

public sealed record IndexKeyColumnDto
{
    public required string Name { get; init; }
    public required bool IsDescending { get; init; }
}

public sealed record IndexDto
{
    public required string Name { get; init; }
    public required string TypeDesc { get; init; }
    public required bool IsUnique { get; init; }
    public required bool IsPrimaryKey { get; init; }
    public required bool IsDisabled { get; init; }
    public string? FilterDefinition { get; init; }
    public required IReadOnlyList<IndexKeyColumnDto> KeyColumns { get; init; }
    public required IReadOnlyList<string> IncludedColumns { get; init; }
    public int? FillFactor { get; init; }
}

public sealed record PrimaryKeyDto
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required bool IsClustered { get; init; }
}

public sealed record CheckConstraintDto
{
    public required string Name { get; init; }
    public required string Definition { get; init; }
    public required bool IsDisabled { get; init; }
    public required bool IsNotTrusted { get; init; }
}

public sealed record TriggerDto
{
    public required string Name { get; init; }
    public required bool IsDisabled { get; init; }
    public required bool IsInsteadOfTrigger { get; init; }
    public required IReadOnlyList<string> Events { get; init; }
}

public sealed record StatisticDto
{
    public required string Name { get; init; }
    public required bool IsAutoCreated { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }
    public int? RowsSampled { get; init; }
}

public sealed record DescribeTableDto
{
    public required ObjectRefDto Object { get; init; }
    public long? RowCountEstimate { get; init; }
    public double? SizeKb { get; init; }
    public required bool IsMemoryOptimized { get; init; }
    public required bool IsTemporal { get; init; }
    public string? TemporalHistoryTable { get; init; }
    public required bool IsChangeTrackingEnabled { get; init; }
    public string? PartitionScheme { get; init; }
    public string? FileGroup { get; init; }
    public required IReadOnlyList<ColumnDto> Columns { get; init; }
    public PrimaryKeyDto? PrimaryKey { get; init; }
    public required IReadOnlyList<IndexDto> Indexes { get; init; }
    public required IReadOnlyList<ForeignKeyRowDto> ForeignKeysOutbound { get; init; }
    public required IReadOnlyList<ForeignKeyRowDto> ForeignKeysInbound { get; init; }
    public required IReadOnlyList<CheckConstraintDto> CheckConstraints { get; init; }
    public required IReadOnlyList<TriggerDto> Triggers { get; init; }
    public required IReadOnlyList<StatisticDto> Statistics { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed record DescribeTableRequest(
    string ServerName,
    string DatabaseName,
    string Schema,
    string Name,
    bool IncludeIndexes,
    bool IncludeForeignKeys,
    bool IncludeTriggers,
    bool IncludeCheckConstraints,
    bool IncludeStatistics)
    : IRequest<DescribeTableDto?>;

public sealed class DescribeTableHandler(ITablePort tablePort, IDatabasePort databasePort)
    : IRequestHandler<DescribeTableRequest, DescribeTableDto?>
{
    private readonly ITablePort _tablePort = tablePort;
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DescribeTableDto?> Handle(DescribeTableRequest request, CancellationToken cancellationToken)
    {
        Table? table = await _tablePort.GetTable(
            request.ServerName, request.DatabaseName, request.Schema, request.Name, cancellationToken);

        if (table is null)
        {
            return null;
        }

        List<IndexDto> allIndexes = TableViewMappers.MapIndexes(table.Indexes);
        PrimaryKeyDto? primaryKey = BuildPrimaryKey(allIndexes);
        List<IndexDto> indexes = request.IncludeIndexes ? allIndexes : [];

        List<ForeignKeyRowDto> outbound = [];
        List<ForeignKeyRowDto> inbound = [];
        if (request.IncludeForeignKeys)
        {
            Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);
            outbound = MapOutboundForeignKeys(table, database, request.DatabaseName);
            inbound = MapInboundForeignKeys(table, database, request.DatabaseName);
        }

        List<CheckConstraintDto> checks = request.IncludeCheckConstraints ? MapChecks(table) : [];
        List<TriggerDto> triggers = request.IncludeTriggers ? TableViewMappers.MapTriggers(table.Triggers) : [];

        List<StatisticDto> statistics = [];
        List<string> warnings = [];
        if (request.IncludeStatistics)
        {
            statistics = TableViewMappers.MapStatistics(table.Statistics);
            if (statistics.Count > 0)
            {
                warnings.Add(
                    "statistics[].last_updated and statistics[].rows_sampled are not available via SMO without " +
                    "a DMV query (sys.dm_db_stats_properties); both are always null.");
            }
        }

        return new DescribeTableDto
        {
            Object = BuildObjectRef(request.DatabaseName, table),
            RowCountEstimate = (long)table.RowCountAsDouble,
            SizeKb = table.DataSpaceUsed + table.IndexSpaceUsed,
            IsMemoryOptimized = table.IsMemoryOptimized,
            IsTemporal = table.IsSystemVersioned,
            TemporalHistoryTable = table.IsSystemVersioned
                ? Identifiers.BuildQualifiedName(table.HistoryTableSchema, table.HistoryTableName)
                : null,
            IsChangeTrackingEnabled = table.ChangeTrackingEnabled,
            PartitionScheme = string.IsNullOrEmpty(table.PartitionScheme) ? null : table.PartitionScheme,
            FileGroup = string.IsNullOrEmpty(table.FileGroup) ? null : table.FileGroup,
            Columns = TableViewMappers.MapColumns(table.Columns),
            PrimaryKey = primaryKey,
            Indexes = indexes,
            ForeignKeysOutbound = outbound,
            ForeignKeysInbound = inbound,
            CheckConstraints = checks,
            Triggers = triggers,
            Statistics = statistics,
            Warnings = warnings,
        };
    }

    private static ObjectRefDto BuildObjectRef(string database, Table table) => new()
    {
        Database = database,
        Schema = table.Schema,
        Name = table.Name,
        ObjectId = table.ID,
        TypeDesc = "USER_TABLE",
        Fqn = Identifiers.BuildFqn(database, table.Schema, table.Name),
    };

    private static ObjectRefDto BuildUnresolvedObjectRef(string database, string schema, string name) => new()
    {
        Database = database,
        Schema = schema,
        Name = name,
        ObjectId = 0,
        TypeDesc = "UNKNOWN",
        Fqn = Identifiers.BuildFqn(database, schema, name),
    };

    private static PrimaryKeyDto? BuildPrimaryKey(IReadOnlyList<IndexDto> indexes)
    {
        IndexDto? pkIndex = indexes.FirstOrDefault(i => i.IsPrimaryKey);
        if (pkIndex is null)
        {
            return null;
        }

        return new PrimaryKeyDto
        {
            Name = pkIndex.Name,
            Columns = pkIndex.KeyColumns.Select(c => c.Name).ToArray(),
            IsClustered = pkIndex.TypeDesc == "INDEX_CLUSTERED",
        };
    }

    private List<ForeignKeyRowDto> MapOutboundForeignKeys(Table table, Database database, string databaseName)
    {
        List<ForeignKeyRowDto> result = new(table.ForeignKeys.Count);
        ObjectRefDto from = BuildObjectRef(databaseName, table);

        foreach (ForeignKey fk in table.ForeignKeys.Cast<ForeignKey>())
        {
            Table? referencedTable = database.Tables[fk.ReferencedTable, fk.ReferencedTableSchema];
            ObjectRefDto to = referencedTable is not null
                ? BuildObjectRef(databaseName, referencedTable)
                : BuildUnresolvedObjectRef(databaseName, fk.ReferencedTableSchema, fk.ReferencedTable);

            result.Add(MapForeignKey(fk, from, to));
        }

        return result;
    }

    private List<ForeignKeyRowDto> MapInboundForeignKeys(Table table, Database database, string databaseName)
    {
        List<ForeignKeyRowDto> result = [];
        ObjectRefDto to = BuildObjectRef(databaseName, table);

        foreach (Table other in database.Tables.Cast<Table>())
        {
            foreach (ForeignKey fk in other.ForeignKeys.Cast<ForeignKey>())
            {
                if (string.Equals(fk.ReferencedTableSchema, table.Schema, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(fk.ReferencedTable, table.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ObjectRefDto from = BuildObjectRef(databaseName, other);
                    result.Add(MapForeignKey(fk, from, to));
                }
            }
        }

        return result;
    }

    private static ForeignKeyRowDto MapForeignKey(ForeignKey fk, ObjectRefDto from, ObjectRefDto to)
    {
        ForeignKeyColumn[] fkColumns = fk.Columns.Cast<ForeignKeyColumn>().ToArray();

        return new ForeignKeyRowDto
        {
            Name = fk.Name,
            From = from,
            To = to,
            FromColumns = fkColumns.Select(c => c.Name).ToArray(),
            ToColumns = fkColumns.Select(c => c.ReferencedColumn).ToArray(),
            DeleteAction = MapForeignKeyAction(fk.DeleteAction),
            UpdateAction = MapForeignKeyAction(fk.UpdateAction),
            IsDisabled = false,
            IsNotTrusted = !fk.IsChecked,
        };
    }

    private static string MapForeignKeyAction(ForeignKeyAction action) => action switch
    {
        ForeignKeyAction.Cascade => "CASCADE",
        ForeignKeyAction.SetNull => "SET_NULL",
        ForeignKeyAction.SetDefault => "SET_DEFAULT",
        _ => "NO_ACTION",
    };

    private static List<CheckConstraintDto> MapChecks(Table table) =>
        table.Checks.Cast<Check>()
            .Select(c => new CheckConstraintDto
            {
                Name = c.Name,
                Definition = c.Text,
                IsDisabled = !c.IsEnabled,
                IsNotTrusted = !c.IsChecked,
            })
            .ToList();
}
