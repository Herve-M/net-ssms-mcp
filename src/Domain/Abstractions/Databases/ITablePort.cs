using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface ITablePort
{
    Task<IReadOnlyCollection<Table>> GetDatabaseTables(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseTablesCount(string serverName, string databaseName, CancellationToken cancellationToken);

    Task<Table?> GetTable(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InboundForeignKeyReference>> GetInboundForeignKeyReferences(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken);
}

public sealed record InboundForeignKeyReference(string Schema, string Name, string ForeignKeyName);