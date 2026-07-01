namespace ssmsmcp.Infrastructure.Integration.Fixtures;

/// <summary>The SQL Server versions exercised by the integration tests.</summary>
public enum SqlServerVersion
{
    Sql2022,
    Sql2025,
}

/// <summary>Static metadata for a <see cref="SqlServerVersion"/>: how to build/run its
/// container image and what to assert against the running instance.</summary>
public sealed record SqlServerImageSpec(
    SqlServerVersion Version,
    string Dockerfile,
    string RestoreScriptPath,
    string DatabaseName,
    int ExpectedMajorVersion,
    int VersionYear)
{
    public static readonly SqlServerImageSpec Sql2022 = new(
        Version: SqlServerVersion.Sql2022,
        Dockerfile: "sql-2022.dockerfile",
        RestoreScriptPath: "/usr/config/restore-2022.sql",
        DatabaseName: "AdventureWorksLT2022",
        ExpectedMajorVersion: 16,
        VersionYear: 2022);

    public static readonly SqlServerImageSpec Sql2025 = new(
        Version: SqlServerVersion.Sql2025,
        Dockerfile: "sql-2025.dockerfile",
        RestoreScriptPath: "/usr/config/restore-2025.sql",
        DatabaseName: "AdventureWorksLT2025",
        ExpectedMajorVersion: 17,
        VersionYear: 2025);

    public static IReadOnlyList<SqlServerImageSpec> All { get; } = [Sql2022, Sql2025];

    public static SqlServerImageSpec For(SqlServerVersion version) =>
        version == SqlServerVersion.Sql2022 ? Sql2022 : Sql2025;
}
