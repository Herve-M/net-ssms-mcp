using System.Data;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class StoredProcedureAdapter(IDatabasePort databasePort) : IStoredProcedurePort
{
    public async Task<IReadOnlyCollection<StoredProcedure>> GetDatabaseStoredProcedures(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.StoredProcedures
            .Cast<StoredProcedure>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseStoredProceduresCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.StoredProcedures.Count;
    }

    public async Task<StoredProcedure?> GetStoredProcedure(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.StoredProcedures[name, schema];
    }

    public async Task<IReadOnlyCollection<FirstResultSetColumnInfo>> DescribeFirstResultSet(string serverName, string databaseName, int objectId, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        string sql =
            "SELECT column_ordinal, name, is_nullable, system_type_name, error_number " +
            $"FROM sys.dm_exec_describe_first_result_set_for_object({objectId}, 0) " +
            "ORDER BY column_ordinal";

        DataSet dataSet = database.ExecuteWithResults(sql);

        List<FirstResultSetColumnInfo> result = [];
        if (dataSet.Tables.Count > 0)
        {
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                result.Add(new FirstResultSetColumnInfo(
                    Ordinal: row["column_ordinal"] is int ordinal ? ordinal : 0,
                    Name: row["name"] as string,
                    SystemTypeName: row["system_type_name"] as string,
                    IsNullable: row["is_nullable"] as bool?,
                    ErrorNumber: row["error_number"] is int errorNumber ? errorNumber : 0));
            }
        }

        return result;
    }
}