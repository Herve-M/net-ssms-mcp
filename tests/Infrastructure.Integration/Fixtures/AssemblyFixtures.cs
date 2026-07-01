using ssmsmcp.Infrastructure.Integration.Fixtures;

// Builds/starts both SQL Server containers once for the whole test assembly and makes the
// fixture available to any test class via constructor injection (xUnit v3 assembly fixture).
[assembly: AssemblyFixture(typeof(SqlServerFixture))]
