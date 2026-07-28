# Security boundary (Application)

Read-only security-surface tools backed by SMO principal/permission catalogs.

## Slices

- `ListPrincipals` — flat paginated list of logins, users, server roles, database roles (`principal_kind` discriminator). SPEC §7.17.
- `ListRoleMemberships` — direct role→member edges (server + database). `include_transitive` reserved/not applied. SPEC §7.18.
- `ListPermissions` — direct granted permissions by principal/securable. `include_inherited` reserved/not applied. SPEC §7.20.

## Conventions

- Pagination: `PageRequest`/`PagedResult<T>` (page/page_size, max 100).
- SMO→DTO mapping lives in the handler; ports return raw SMO collections (`Microsoft.SqlServer.Management.Smo` is a sanctioned Domain-layer extension).
- SMO-first; no raw SQL.
- Deferred (later cross-cutting pass): result envelope, page_token cursors, structured error envelope, CSV mirror, transitive/inherited expansion.

## Depends on

- `ssmsmcp.Domain.Abstractions.Security.*` ports → `ssmsmcp.Infrastructure.SSMS.*Adapter` (SMO).
