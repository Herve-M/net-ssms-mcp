IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-sa-password", secret: true);

IResourceBuilder<SqlServerServerResource> sqlServer2022 = builder
    .AddSqlServer("sql-2022", sqlPassword, port: 1422)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDockerfile("dockers", "sql-2022.dockerfile")
        .WithDataVolume("sql_data_22")
        .WithBindMount("../../tests/data", "/var/opt/mssql/backup", isReadOnly: true)
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_PID", "Developer")
        .WithEnvironment("MSSQL_DB_BAK", "/usr/config/restore-2022.sql")
    ;

IResourceBuilder<SqlServerServerResource> sqlServer2025 = builder
    .AddSqlServer("sql-2025", sqlPassword, port: 1425)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDockerfile("dockers", "sql-2025.dockerfile")
        .WithDataVolume("sql_data_25")
        .WithBindMount("../../tests/data", "/var/opt/mssql/backup", isReadOnly: true)
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_PID", "Developer")
        .WithEnvironment("MSSQL_DB_BAK", "/usr/config/restore-2025.sql")
    ;

IResourceBuilder<ProjectResource> mcpServer = builder.AddProject<Projects.Server_Api>("http-api")
    .WithReference(sqlServer2022)
        .WaitFor(sqlServer2022)
    .WithReference(sqlServer2025)
        .WaitFor(sqlServer2025)
    .WithExplicitStart()
    ;

builder.AddContainer("inspector", "ghcr.io/modelcontextprotocol/inspector", "latest")
    .WithHttpEndpoint(port: 6274, targetPort: 6274, name: "client")
    .WithHttpEndpoint(port: 6277, targetPort: 6277, name: "proxy")
    .WithEnvironment("HOST", "0.0.0.0")
    .WithEnvironment("DANGEROUSLY_OMIT_AUTH", "true")
    .WithEnvironment("MCP_AUTO_OPEN_ENABLED", "false")
    .WithEnvironment("ALLOWED_ORIGINS", "http://0.0.0.0:6274")
    .WithReference(mcpServer)
        .WaitFor(mcpServer)
    .WithExplicitStart()
    ;

builder.Build().Run();
