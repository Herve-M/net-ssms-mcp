using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseConfigurationDto
{
    public string Collation { get; init; }
    public string CompatibilityLevel { get; init; }
    public string ContainmentType { get; init; }
    public string RecoveryModel { get; init; }
    public bool AnsiNullDefault { get; init; }
    public bool AnsiNullsEnabled { get; init; }
    public bool AnsiPaddingEnabled { get; init; }
    public bool AnsiWarningsEnabled { get; init; }
    public bool ArithmeticAbortEnabled { get; init; }
    public bool AutoClose { get; init; }
    public bool AutoShrink { get; init; }
    public bool AutoUpdateStatisticsEnabled { get; init; }
    public bool AutoUpdateStatisticsAsync { get; init; }
    public bool AutoCreateStatisticsEnabled { get; init; }
    public bool AutoCreateIncrementalStatisticsEnabled { get; init; }
    public bool IsParameterizationForced { get; init; }
    public bool RecursiveTriggersEnabled { get; init; }
    public bool NestedTriggersEnabled { get; init; }
    public string PageVerify { get; init; }
    public bool QuotedIdentifiersEnabled { get; init; }
    public bool NumericRoundAbortEnabled { get; init; }
}

public sealed record GetDatabaseConfigurationRequest(string ServerName, string DatabaseName) : IRequest<DatabaseConfigurationDto>;

public sealed class GetDatabaseConfigurationHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseConfigurationRequest, DatabaseConfigurationDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseConfigurationDto> Handle(GetDatabaseConfigurationRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        return new DatabaseConfigurationDto
        {
            Collation = database.Collation,
            CompatibilityLevel = database.CompatibilityLevel.ToString(),
            ContainmentType = database.ContainmentType.ToString(),
            RecoveryModel = database.RecoveryModel.ToString(),
            AnsiNullDefault = database.AnsiNullDefault,
            AnsiNullsEnabled = database.AnsiNullsEnabled,
            AnsiPaddingEnabled = database.AnsiPaddingEnabled,
            AnsiWarningsEnabled = database.AnsiWarningsEnabled,
            ArithmeticAbortEnabled = database.ArithmeticAbortEnabled,
            AutoClose = database.AutoClose,
            AutoShrink = database.AutoShrink,
            AutoUpdateStatisticsEnabled = database.AutoUpdateStatisticsEnabled,
            AutoUpdateStatisticsAsync = database.AutoUpdateStatisticsAsync,
            AutoCreateStatisticsEnabled = database.AutoCreateStatisticsEnabled,
            AutoCreateIncrementalStatisticsEnabled = database.AutoCreateIncrementalStatisticsEnabled,
            IsParameterizationForced = database.IsParameterizationForced,
            RecursiveTriggersEnabled = database.RecursiveTriggersEnabled,
            NestedTriggersEnabled = database.NestedTriggersEnabled,
            PageVerify = database.PageVerify.ToString(),
            QuotedIdentifiersEnabled = database.QuotedIdentifiersEnabled,
            NumericRoundAbortEnabled = database.NumericRoundAbortEnabled
        };
    }
}