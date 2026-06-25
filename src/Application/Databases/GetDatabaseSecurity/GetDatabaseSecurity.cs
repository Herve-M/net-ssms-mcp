using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseSecurityDto
{
    public string Owner { get; init; }
    public string UserAccess { get; init; }
    public bool Trustworthy { get; init; }
    public bool DatabaseOwnershipChaining { get; init; }
    public bool EncryptionEnabled { get; init; }
    public bool HasDatabaseEncryptionKey { get; init; }
    public bool DboLogin { get; init; }
    public bool IsDbOwner { get; init; }
    public bool IsDbSecurityAdmin { get; init; }
    public bool IsDbAccessAdmin { get; init; }
    public bool IsDbDatareader { get; init; }
    public bool IsDbDatawriter { get; init; }
    public bool IsDbDdlAdmin { get; init; }
}

public sealed record GetDatabaseSecurityRequest(string ServerName, string DatabaseName) : IRequest<DatabaseSecurityDto>;

public sealed class GetDatabaseSecurityHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseSecurityRequest, DatabaseSecurityDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseSecurityDto> Handle(GetDatabaseSecurityRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        return new DatabaseSecurityDto
        {
            Owner = database.Owner,
            UserAccess = database.UserAccess.ToString(),
            Trustworthy = database.Trustworthy,
            DatabaseOwnershipChaining = database.DatabaseOwnershipChaining,
            EncryptionEnabled = database.EncryptionEnabled,
            HasDatabaseEncryptionKey = database.HasDatabaseEncryptionKey,
            DboLogin = database.DboLogin,
            IsDbOwner = database.IsDbOwner,
            IsDbSecurityAdmin = database.IsDbSecurityAdmin,
            IsDbAccessAdmin = database.IsDbAccessAdmin,
            IsDbDatareader = database.IsDbDatareader,
            IsDbDatawriter = database.IsDbDatawriter,
            IsDbDdlAdmin = database.IsDbDdlAdmin
        };
    }
}