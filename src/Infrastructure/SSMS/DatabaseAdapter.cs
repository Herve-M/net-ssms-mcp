using Microsoft.Extensions.Caching.Memory;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Infrastructure.Abstractions.SSMS;

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
}
