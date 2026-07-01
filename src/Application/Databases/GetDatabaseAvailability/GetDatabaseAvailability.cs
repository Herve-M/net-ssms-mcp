using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseAvailabilityDto
{
    public string AvailabilityGroupName { get; init; }
    public string AvailabilityDatabaseSynchronizationState { get; init; }
    public bool IsMirroringEnabled { get; init; }
    public string MirroringStatus { get; init; }
    public string MirroringPartner { get; init; }
    public Guid ServiceBrokerGuid { get; init; }
    public bool BrokerEnabled { get; init; }
    public string ReplicationOptions { get; init; }
    public string LogReuseWaitStatus { get; init; }
}

public sealed record GetDatabaseAvailabilityRequest(string ServerName, string DatabaseName) : IRequest<DatabaseAvailabilityDto>;

public sealed class GetDatabaseAvailabilityHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseAvailabilityRequest, DatabaseAvailabilityDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseAvailabilityDto> Handle(GetDatabaseAvailabilityRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        return new DatabaseAvailabilityDto
        {
            AvailabilityGroupName = database.AvailabilityGroupName,
            //TOSEE: based on AvailabilityGroupName?
            // AvailabilityDatabaseSynchronizationState = database.AvailabilityDatabaseSynchronizationState.ToString(),
            IsMirroringEnabled = database.IsMirroringEnabled,
            MirroringStatus = database.MirroringStatus.ToString(),
            MirroringPartner = database.MirroringPartner,
            ServiceBrokerGuid = database.ServiceBrokerGuid,
            BrokerEnabled = database.BrokerEnabled,
            ReplicationOptions = database.ReplicationOptions.ToString(),
            LogReuseWaitStatus = database.LogReuseWaitStatus.ToString()
        };
    }
}