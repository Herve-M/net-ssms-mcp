using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Tables;
using Index = Microsoft.SqlServer.Management.Smo.Index;

namespace ssmsmcp.Application.Databases;

internal static class TableViewMappers
{
    public static List<ColumnDto> MapColumns(ColumnCollection columns)
    {
        List<ColumnDto> result = new(columns.Count);
        foreach (Column column in columns.Cast<Column>().OrderBy(c => c.ID))
        {
            result.Add(new ColumnDto
            {
                Name = column.Name,
                Ordinal = column.ID,
                DataType = FormatDataType(column.DataType),
                IsNullable = column.Nullable,
                IsIdentity = column.Identity,
                IsComputed = column.Computed,
                ComputedDefinition = column.Computed ? column.ComputedText : null,
                IsPersisted = column.Computed ? column.IsPersisted : null,
                DefaultConstraint = column.DefaultConstraint?.Text,
                Collation = string.IsNullOrEmpty(column.Collation) ? null : column.Collation,
                IsMasked = column.IsMasked,
                MaskFunction = column.IsMasked ? column.MaskingFunction : null,
            });
        }

        return result;
    }

    public static string FormatDataType(DataType dataType)
    {
        if (dataType.SqlDataType is SqlDataType.NVarCharMax or SqlDataType.VarCharMax or SqlDataType.VarBinaryMax)
        {
            return $"{dataType.Name}(max)";
        }

        bool isCharLike = dataType.SqlDataType is SqlDataType.Char or SqlDataType.VarChar
            or SqlDataType.NChar or SqlDataType.NVarChar or SqlDataType.Binary or SqlDataType.VarBinary;
        if (isCharLike && dataType.MaximumLength > 0)
        {
            bool isDoubleByte = dataType.SqlDataType is SqlDataType.NChar or SqlDataType.NVarChar;
            int length = isDoubleByte ? dataType.MaximumLength / 2 : dataType.MaximumLength;
            return $"{dataType.Name}({length})";
        }

        bool isNumeric = dataType.SqlDataType is SqlDataType.Decimal or SqlDataType.Numeric;
        if (isNumeric)
        {
            return $"{dataType.Name}({dataType.NumericPrecision},{dataType.NumericScale})";
        }

        return dataType.Name;
    }

    public static List<IndexDto> MapIndexes(IndexCollection indexes)
    {
        List<IndexDto> result = new(indexes.Count);
        foreach (Index index in indexes.Cast<Index>())
        {
            IndexedColumn[] indexedColumns = index.IndexedColumns.Cast<IndexedColumn>().ToArray();

            result.Add(new IndexDto
            {
                Name = index.Name,
                TypeDesc = index.IsClustered ? "INDEX_CLUSTERED" : "INDEX_NONCLUSTERED",
                IsUnique = index.IsUnique,
                IsPrimaryKey = index.IndexKeyType == IndexKeyType.DriPrimaryKey,
                IsDisabled = index.IsDisabled,
                FilterDefinition = index.HasFilter ? index.FilterDefinition : null,
                KeyColumns = indexedColumns
                    .Where(c => !c.IsIncluded)
                    .Select(c => new IndexKeyColumnDto { Name = c.Name, IsDescending = c.Descending })
                    .ToArray(),
                IncludedColumns = indexedColumns
                    .Where(c => c.IsIncluded)
                    .Select(c => c.Name)
                    .ToArray(),
                FillFactor = index.FillFactor == 0 ? null : index.FillFactor,
            });
        }

        return result;
    }

    public static List<TriggerDto> MapTriggers(TriggerCollection triggers) =>
        triggers.Cast<Trigger>()
            .Select(t => new TriggerDto
            {
                Name = t.Name,
                IsDisabled = !t.IsEnabled,
                IsInsteadOfTrigger = t.InsteadOf,
                Events = BuildTriggerEvents(t),
            })
            .ToList();

    private static List<string> BuildTriggerEvents(Trigger trigger)
    {
        List<string> events = [];
        if (trigger.Insert)
        {
            events.Add("INSERT");
        }

        if (trigger.Update)
        {
            events.Add("UPDATE");
        }

        if (trigger.Delete)
        {
            events.Add("DELETE");
        }

        return events;
    }

    public static List<StatisticDto> MapStatistics(StatisticCollection statistics) =>
        statistics.Cast<Statistic>()
            .Select(s => new StatisticDto
            {
                Name = s.Name,
                IsAutoCreated = s.IsAutoCreated,
                Columns = s.StatisticColumns.Cast<StatisticColumn>().Select(c => c.Name).ToArray(),
                LastUpdated = null,
                RowsSampled = null,
            })
            .ToList();
}
