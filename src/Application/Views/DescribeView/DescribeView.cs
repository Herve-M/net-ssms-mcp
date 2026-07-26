using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions;
using ssmsmcp.Application.Tables;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Views;

public sealed record DescribeViewDto
{
    public required ObjectRefDto Object { get; init; }
    public string? Definition { get; init; }
    public required bool IsEncrypted { get; init; }
    public required bool IsSchemaBound { get; init; }
    public required bool HasIndex { get; init; }
    public long? RowCountEstimate { get; init; }
    public double? SizeKb { get; init; }
    public required IReadOnlyList<ColumnDto> Columns { get; init; }
    public required IReadOnlyList<IndexDto> Indexes { get; init; }
    public required IReadOnlyList<TriggerDto> Triggers { get; init; }
    public required IReadOnlyList<StatisticDto> Statistics { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

public sealed record DescribeViewRequest(
    string ServerName,
    string DatabaseName,
    string Schema,
    string Name,
    bool IncludeIndexes,
    bool IncludeTriggers,
    bool IncludeStatistics)
    : IRequest<DescribeViewDto?>;

public sealed class DescribeViewHandler(IViewPort viewPort)
    : IRequestHandler<DescribeViewRequest, DescribeViewDto?>
{
    private readonly IViewPort _viewPort = viewPort;

    public async ValueTask<DescribeViewDto?> Handle(DescribeViewRequest request, CancellationToken cancellationToken)
    {
        View? view = await _viewPort.GetView(
            request.ServerName, request.DatabaseName, request.Schema, request.Name, cancellationToken);

        if (view is null)
        {
            return null;
        }

        List<IndexDto> indexes = request.IncludeIndexes ? TableViewMappers.MapIndexes(view.Indexes) : [];
        List<TriggerDto> triggers = request.IncludeTriggers ? TableViewMappers.MapTriggers(view.Triggers) : [];

        List<StatisticDto> statistics = [];
        List<string> warnings = [];
        if (request.IncludeStatistics)
        {
            statistics = TableViewMappers.MapStatistics(view.Statistics);
            if (statistics.Count > 0)
            {
                warnings.Add(
                    "statistics[].last_updated and statistics[].rows_sampled are not available via SMO without " +
                    "a DMV query (sys.dm_db_stats_properties); both are always null.");
            }
        }

        string? definition = view.TextBody;
        if (view.IsEncrypted)
        {
            definition = null;
            warnings.Add("definition is null because the view is encrypted (WITH ENCRYPTION).");
        }

        return new DescribeViewDto
        {
            Object = new ObjectRefDto
            {
                Database = request.DatabaseName,
                Schema = view.Schema,
                Name = view.Name,
                ObjectId = view.ID,
                TypeDesc = "VIEW",
                Fqn = Identifiers.BuildFqn(request.DatabaseName, view.Schema, view.Name),
            },
            Definition = definition,
            IsEncrypted = view.IsEncrypted,
            IsSchemaBound = view.IsSchemaBound,
            HasIndex = view.HasIndex,
            RowCountEstimate = null,
            SizeKb = null,
            Columns = TableViewMappers.MapColumns(view.Columns),
            Indexes = indexes,
            Triggers = triggers,
            Statistics = statistics,
            Warnings = warnings,
        };
    }
}
