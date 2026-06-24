using AwesomeAssertions;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Server.Mcp.Cli;

namespace ssmsmcp.Server.Mcp.Integration.Cli;

public sealed class MainConfigurationFactoryTests
{
    [Fact]
    public void FromServer_WithConnectionString_ReturnsSingleMainDataSource()
    {
        // Arrange
        const string connectionString = "Server=localhost;Database=Db;Trusted_Connection=True;";

        // Act
        MainConfiguration result = MainConfigurationFactory.FromServer(connectionString);

        // Assert
        result.DataSources.Should().ContainSingle();
        result.DataSources[0].Name.Should().Be("main");
        result.DataSources[0].ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void FromConfigFile_WithMultipleSources_KeepsFirstRenamedToMain()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
        {
          "main": {
            "data-source": [
              { "name": "2022", "connectionString": "Server=a;" },
              { "name": "2025", "connectionString": "Server=b;" }
            ]
          }
        }
        """);

        try
        {
            // Act
            MainConfiguration result = MainConfigurationFactory.FromConfigFile(path);

            // Assert
            result.DataSources.Should().ContainSingle();
            result.DataSources[0].Name.Should().Be("main");
            result.DataSources[0].ConnectionString.Should().Be("Server=a;");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromConfigFile_WithMissingFile_Throws()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        // Act
        Action act = () => MainConfigurationFactory.FromConfigFile(path);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void FromConfigFile_WithEmptyDataSources_Throws()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "main": { "data-source": [] } }""");

        try
        {
            // Act
            Action act = () => MainConfigurationFactory.FromConfigFile(path);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
