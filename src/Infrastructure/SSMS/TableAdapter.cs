using System.Data;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class TableAdapter(IDatabasePort databasePort) : ITablePort
{
    public async Task<IReadOnlyCollection<Table>> GetDatabaseTables(
        string serverName,
        string databaseName,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.Tables
            .Cast<Table>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseTablesCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Tables.Count;
    }

    public async Task<Table?> GetTable(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Tables[name, schema];
    }

    public async Task<IReadOnlyCollection<InboundForeignKeyReference>> GetInboundForeignKeyReferences(
        string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        Sfc.Urn urn = new(
            $"{database.Urn}/Table/ForeignKey[@ReferencedTable='{Sfc.Urn.EscapeString(name)}' and @ReferencedTableSchema='{Sfc.Urn.EscapeString(schema)}']");

        Sfc.Request request = new(urn, ["Name"])
        {
            ParentPropertiesRequests =
            [
                new Sfc.PropertiesRequest
                {
                    Fields = ["Schema", "Name"],
                    PropertyAlias = new Sfc.PropertyAlias(["ParentSchema", "ParentName"]),
                },
            ],
        };

        DataTable matches = new Sfc.Enumerator().Process(database.Parent.ConnectionContext, request);

        List<InboundForeignKeyReference> refs = new(matches.Rows.Count);
        foreach (DataRow row in matches.Rows)
        {
            refs.Add(new InboundForeignKeyReference(
                (string)row["ParentSchema"], (string)row["ParentName"], (string)row["Name"]));
        }

        return refs;
    }
}