# net-ssms-mcp

A read-only MCP server that exposes Microsoft SQL Server **metadata** — instance, schema and security — to AI agents, built on the SSMS SDK (SMO); a companion to Data API builder.

[![AI-DECLARATION: pair](https://img.shields.io/badge/䷼%20AI--DECLARATION-pair-ffedd5?labelColor=ffedd5)](./AI-DECLARATION.md)
[![stdio-mcp](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/server-mcp.yml/badge.svg)](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/server-mcp.yml)
[![http-mcp](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/server-api.yml/badge.svg)](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/server-api.yml)
[![codeql-analysis](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/Herve-M/net-ssms-mcp/actions/workflows/codeql-analysis.yml)
[![NuGet](https://img.shields.io/nuget/v/ssms-mcp)](https://github.com/Herve-M/net-ssms-mcp/pkgs/nuget/ssms-mcp
)
[![License: MPL 2.0](https://img.shields.io/badge/License-MPL_2.0-brightgreen.svg)](./LICENSE)

> [!IMPORTANT]
> This project hasn't reached a stable release yet: tool signatures, output formats and behavior may change between versions without notice. It's read-only by design (no DDL/DML execution surface), so there's no write path to break, but everything else should be treated as pre-1.0: test it against your own instance before depending on it.

## Overview

### Tools

Integration tests run each tool against **SQL Server 2022 and 2025** containers seeded with AdventureWorksLT.

| Name                    | Description                                                  | Implemented | Tested (SQL Server variant) |
| ----------------------- | ------------------------------------------------------------ | :---------: | --------------------------- |
| `get_server_info`       | Server version, edition, feature flags, configuration        |      ✅      | 2022, 2025                  |
| `list_databases`        | Databases on the instance with size, status, options         |      ✅      | 2022, 2025                  |
| `list_objects`          | Generic object listing with type filter                      |      ✅      | 2022, 2025                  |
| `describe_table`        | Columns, indexes, FKs, constraints, stats                    |      ✅      | 2022, 2025                  |
| `describe_view`         | View columns and definition                                  |      ✅      | 2022, 2025                  |
| `describe_procedure`    | Parameters, return shape, body (T-SQL) or assembly ref (CLR) |      ✅      | 2022, 2025                  |
| `list_principals`       | Flat, paginated list of logins, users and roles              |      ✅      | 2022, 2025 — stdio only     |
| `list_role_memberships` | Role → member edges, optionally transitive                   |      ✅      | 2022, 2025 — stdio only     |
| `list_permissions`      | Effective and granted permissions                            |      ✅      | 2022, 2025 — stdio only     |
| `get_foreign_keys`      | FK relationships in/out of a table                           |      ❌      | —                           |
| `get_dependencies`      | Recursive dependency tree                                    |      ❌      | —                           |
| `get_dependency_path`   | Paths between two objects                                    |      ❌      | —                           |
| `script_object`         | DDL CREATE/DROP for one object                               |      ❌      | —                           |
| `script_objects`        | DDL bundle for a set of objects, topologically ordered       |      ❌      | —                           |
| `get_top_queries`       | Top queries by various metrics                               |      ❌      | —                           |
| `get_blocking`          | Current blocking chains                                      |      ❌      | —                           |
| `get_wait_stats`        | Wait-stats summary                                           |      ❌      | —                           |
| `get_index_usage`       | Index usage stats and unused indexes                         |      ❌      | —                           |
| `get_missing_indexes`   | Missing-index suggestions                                    |      ❌      | —                           |
| `analyze_db_health`     | Composite health report                                      |      ❌      | —                           |
| `find_orphaned_users`   | Database users whose SID no longer maps to a server login    |      ❌      | —                           |
| `list_jobs`             | SQL Agent jobs, schedules, last run status                   |      ❌      | —                           |
| `list_backups`          | Backup history per database                                  |      ❌      | —                           |

_Planned tools and their contracts are defined in [`docs/SPEC.md`](./docs/SPEC.md)._

## Installation

<!-- TODO -->

## Usage

<!-- TODO -->

## Configuration

<!-- TODO -->

## Development

<!-- TODO -->

## Contributing

<!-- TODO -->

## License

Licensed under the [Mozilla Public License 2.0](./LICENSE).

Tests data (https://github.com/microsoft/sql-server-samples) under the [MIT license](tests/data/license.txt)
