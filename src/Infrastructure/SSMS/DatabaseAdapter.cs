using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using Urn = Microsoft.SqlServer.Management.Sdk.Sfc.Urn;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class DatabaseAdapter(IServerConnectionFactory factory, IMemoryCache cache)
    : IDatabasePort
{

    internal async Task<Database> GetDatabaseInternal(string serverName, string name, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            $"{serverName}:{name}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                Server server = await factory.GetServer(serverName);
                Database database = server.Databases[name];
                database.PrefetchObjects();
                return database;
            }
        );
    }

    public async Task<Database> GetDatabase(string serverName, string name, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        Database database = server.Databases[name];
        database.PrefetchObjects();
        return database;
    }

    public async Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        return server.Databases.Select(x => x).ToList();
    }

    public async Task<IReadOnlyCollection<Schema>> GetDatabaseSchemas(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        Database database = server.Databases[databaseName];

        if (database == null)
        {
            throw new InvalidOperationException($"Database '{databaseName}' not found on server '{serverName}'.");
        }

        return database.Schemas
            .Cast<Schema>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseSchemasCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        Database database = server.Databases[databaseName];

        if (database == null)
        {
            throw new InvalidOperationException($"Database '{databaseName}' not found on server '{serverName}'.");
        }

        return database.Schemas.Count;
    }

    public async Task<IReadOnlyCollection<DatabaseObjectInfo>> GetDatabaseObjects(
        string serverName, string databaseName, DatabaseObjectTypes types, bool forceRefresh, CancellationToken cancellationToken)
    {
        string cacheKey = $"objects:{serverName}:{databaseName}:{(long)types}";

        if (forceRefresh)
        {
            cache.Remove(cacheKey);
        }

        return await cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return await EnumerateObjects(serverName, databaseName, types);
            })
            ?? Array.Empty<DatabaseObjectInfo>();
    }

    private async Task<IReadOnlyCollection<DatabaseObjectInfo>> EnumerateObjects(
        string serverName, string databaseName, DatabaseObjectTypes types)
    {
        Server server = await factory.GetServer(serverName);
        Database? database = server.Databases[databaseName];

        if (database is null)
        {
            return Array.Empty<DatabaseObjectInfo>();
        }

        DataTable table = database.EnumObjects(types, SortOrder.Name);

        List<DatabaseObjectInfo> objects = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            string urnText = row["Urn"] as string ?? string.Empty;
            string schema = row["Schema"] as string ?? string.Empty;
            string name = row["Name"] as string ?? string.Empty;

            if (!string.IsNullOrEmpty(urnText))
            {
                Urn urn = new(urnText);
                schema = urn.GetAttribute("Schema") ?? schema;
                name = urn.GetAttribute("Name") ?? name;
            }

            objects.Add(new DatabaseObjectInfo(
                schema,
                name,
                row["DatabaseObjectTypes"] as string ?? string.Empty,
                urnText));
        }

        return objects;
    }
}
