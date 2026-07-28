# Domain layer — AGENTS.md

Core layer: business contracts (ports), configuration models, and validators. Has no
dependency on other solution layers. Application depends on it; Infrastructure
implements its ports.

## Conventions

- **SMO as the domain model.** `Microsoft.SqlServer.Management.Smo` types (`Server`,
  `Database`, `Schema`, `Table`, …) are treated as the Domain model. Ports return SMO
  types directly; richer domain types live here only when SMO is insufficient.
- **Ports** (hexagonal): interfaces named `I…Port` under `Abstractions/`. Methods are
  async (`Task<T>`) and take a `CancellationToken`. Collection methods are paginated
  with `int skip, int take` and have a paired `…Count(...)` method.
  - *Exception:* enumeration methods whose backing SMO API cannot page server-side
    (e.g. `Database.EnumObjects`) instead return the **full set** and take a
    `bool forceRefresh` (cache-bypass) flag; callers filter and page in memory. See
    `IDatabasePort.GetDatabaseObjects`.
  - *Exception:* the `Abstractions/Security/` ports return **full collections with no
    `skip`/`take` and no `…Count`** — the principal and permission catalogs are read whole
    and the Application handlers filter/page in memory.
  - *Exception:* lookup ports that resolve a single object take `(schema, name)` and return
    a nullable SMO type instead of a page — e.g. `IUserDefinedAggregatePort`,
    `IStoredProcedurePort.GetStoredProcedure`, `ITablePort.GetTable`.
- Register via `DependencyInjection.cs`.

## Boundaries

- **`Abstractions/Databases/`** — per-database ports. `IDatabasePort` (database lookup,
  schemas + count, and generic object enumeration via `GetDatabaseObjects`) plus one
  port per object type: `ITablePort`, `IViewPort`, `IStoredProcedurePort`,
  `IUserDefinedFunctionPort`, `IUserDefinedTypePort`, `IUserDefinedTableTypePort`,
  `IUserDefinedAggregatePort`, `IUserPort`, `ITriggerPort`, `IRolePort`. Each typed port
  exposes a paginated `Get…` + `Get…Count`, and the describe-backing ports add a
  single-object `Get…(schema, name)` lookup returning a nullable SMO type.
  - **`DatabaseObjectInfo`** — a non-SMO record `(Schema, Name, Type, Urn)` carrying one
    `EnumObjects` row. `EnumObjects` yields a `System.Data.DataTable` (not SMO objects),
    so this is one of the few places a richer Domain type is warranted over an SMO type.
  - **`FirstResultSetColumnInfo`** — the second non-SMO record
    `(Ordinal, Name, SystemTypeName, IsNullable, ErrorNumber)`, carrying one
    `sys.dm_exec_describe_first_result_set_for_object` row for `describe_procedure`. SMO
    exposes no equivalent, which is why this is a Domain record and why its adapter is the
    one sanctioned raw-SQL site (see `Infrastructure/AGENTS.md`).
- **`Abstractions/Security/`** — the security catalogs (SPEC §7.17–§7.20). `IPrincipalPort`
  (`GetServerLogins`, `GetServerRoles`, `GetDatabaseUsers`, `GetDatabaseRoles` — SMO `Login`,
  `ServerRole`, `User`, `DatabaseRole`), `IRoleMembershipPort` (server + database membership
  edges), `IPermissionPort` (`GetPermissions(serverName, databaseName?, securableType?)`).
  - Two more non-SMO records live here because they describe catalog *rows*, not objects:
    **`RoleMembershipRecord`** and **`PermissionRecord`**
    `(Principal, PrincipalType, PermissionName, State, Securable, SecurableType, Grantor,
    SecurableSchema?)`, where `State` is `GRANT`/`DENY`/`REVOKE` and `SecurableType` is
    `SERVER`/`DATABASE`/`SCHEMA`/`OBJECT`.
  - These ports are unpaginated by design — see the exception under Conventions.
- **`Abstractions/Servers/`** — `IServerPort` (`GetServer`, `GetServers`, `GetDatabases`).
- **`Abstractions/Configurations/`** — `IMainConfiguration` + `OptionsValidators<T>`.
- **`Configurations/`** — `MainConfiguration` and `MainConfigurationValidator`.

## Port shape (reference)

`IDatabasePort` (`Abstractions/Databases/IDatabasePort.cs`):

```csharp
Task<Database> GetDatabase(string serverName, string name, CancellationToken ct);
Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken ct);
Task<IReadOnlyCollection<Schema>> GetDatabaseSchemas(string serverName, string databaseName, int skip, int take, CancellationToken ct);
Task<int> GetDatabaseSchemasCount(string serverName, string databaseName, CancellationToken ct);

// Generic object enumeration (full set; caller filters/pages in memory):
Task<IReadOnlyCollection<DatabaseObjectInfo>> GetDatabaseObjects(
    string serverName, string databaseName, DatabaseObjectTypes types, bool forceRefresh, CancellationToken ct);
```

New object-type ports follow the same `Get…(… , int skip, int take, ct)` + `Get…Count(…)`
shape, returning `IReadOnlyCollection<TSmoType>`. `GetDatabaseObjects` is the deliberate
exception (see Conventions): `DatabaseObjectTypes` is the SMO `[Flags]` enum naming which
object types to enumerate.
