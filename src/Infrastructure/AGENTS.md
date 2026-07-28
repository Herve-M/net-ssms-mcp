# Infrastructure layer — AGENTS.md

Implements Domain ports and external concerns: SQL Server connectivity via the SSMS
SDK (SMO), caching, OpenTelemetry, health checks, service discovery, feature toggles,
runtime configuration. Depends on `Domain` (and `Application` contracts where needed);
referenced by the presentation hosts.

## Conventions

- **Adapters** implement Domain `I…Port` interfaces, named `…Adapter`, and are
  `internal sealed`. Dependencies come via primary-constructor injection.
- SMO access goes through `IServerConnectionFactory.GetServer(serverName)`
  (`Abstractions/SSMS/`, impl in `SSMS/Internals/ServerConnectionFactory.cs`).
- Adapters are registered as **singletons**.
- Use `ILogger<T>` for logging. Don't modify `eng/*.props`.

## Layout

- **`SSMS/`** — adapters: `ServerAdapter` (`IServerPort`), `DatabaseAdapter`
  (`IDatabasePort`), one adapter per object-type port (`TableAdapter`,
  `ViewAdapter`, `StoredProcedureAdapter`, `UserDefinedFunctionAdapter`,
  `UserDefinedTypeAdapter`, `UserDefinedTableTypeAdapter`, `UserDefinedAggregateAdapter`,
  `UserAdapter`, `TriggerAdapter`, `RoleAdapter`), and the security adapters
  (`PrincipalAdapter`, `PermissionAdapter`, `RoleMembershipAdapter`).
  `SSMS/Internals/` holds the connection factory.
- **`Configurations/DependencyInjectionBuilder.cs`** — the fluent
  `IInfrastructureDependencyInjectionBuilder` implementation. `WithSSMS()` calls
  `AddMemoryCache()` and registers `IServerConnectionFactory` + every `I…Port → …Adapter`.
  Other steps: `WithOpenTelemetry`, `WithServiceDiscovery`, `WithFeatureToggle`,
  `WithHealthChecks`, `WithRuntimeConfiguration`.
- **`Abstractions/`** — `Configurations/` (builder interfaces + base,
  `OpenTelemetrySettings`) and `SSMS/IServerConnectionFactory`.
- **`DependencyInjection.cs`** — `UseInfrastructureLayer(...)` entry point returning the
  builder; hosts chain the `With…()` steps then `.Build()`.

## Adapter pattern

`DatabaseAdapter` (`SSMS/DatabaseAdapter.cs`) is the reference: resolve the `Server` via
the factory, index into `server.Databases[name]`, call `database.PrefetchObjects()`,
then `.Cast<T>().Skip(skip).Take(take).ToList()` for paginated reads and `.Count` for
counts. Throw `InvalidOperationException` when the database is not found. `GetDatabase`
results may be cached in `IMemoryCache` (15-minute absolute expiration).

## Raw SQL — the one sanctioned exception

`StoredProcedureAdapter.DescribeFirstResultSet` is the **only**
`database.ExecuteWithResults(...)` call in the codebase. It is permitted because SMO exposes
no equivalent to `sys.dm_exec_describe_first_result_set_for_object`, which
`describe_procedure` needs for optional result-set introspection. It interpolates the numeric
`objectId` only — never a caller-supplied identifier — and maps rows to
`FirstResultSetColumnInfo`.

Before adding a second raw-SQL site, read [`sql-access.md`](../../.claude/rules/sql-access.md):
SMO first, and identifiers must not reach SQL text.

## Generic object listing

`GetDatabaseObjects` is the generic object-listing read and departs from the adapter pattern
above: it enumerates with `database.EnumObjects(types, SortOrder.Name)`, derives
`Schema`/`Name` from each row's `Urn` (the flat `Schema` column is empty in current SMO —
`using Urn = Microsoft.SqlServer.Management.Sdk.Sfc.Urn`, aliased to avoid a `Schema`/
`DatabaseObjectInfo` name clash with that namespace), and caches the full
`IReadOnlyCollection<DatabaseObjectInfo>` in `IMemoryCache` keyed by
`objects:{server}:{db}:{(long)types}` (15-minute absolute expiration). `forceRefresh`
evicts the key before re-enumerating. A missing database returns an **empty collection**
(no throw), so the result is the same shape whether the database is absent or empty.
