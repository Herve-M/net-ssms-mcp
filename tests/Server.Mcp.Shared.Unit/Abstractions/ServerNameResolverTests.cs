using AwesomeAssertions;
using ssmsmcp.Server.Mcp.Shared.Abstractions;

namespace ssmsmcp.Server.Mcp.Shared.Unit.Abstractions;

public class ServerNameResolverTests
{
    [Fact]
    public void TryResolve_WithRequestedName_ReturnsTrueAndUsesRequested()
    {
        bool resolved = ServerNameResolver.TryResolve("prod", new DefaultServerName("main"), out string serverName);

        resolved.Should().BeTrue();
        serverName.Should().Be("prod");
    }

    [Fact]
    public void TryResolve_PrefersRequestedOverDefault()
    {
        bool resolved = ServerNameResolver.TryResolve("prod", new DefaultServerName(null), out string serverName);

        resolved.Should().BeTrue();
        serverName.Should().Be("prod");
    }

    [Fact]
    public void TryResolve_WithoutRequestedName_FallsBackToDefault()
    {
        bool resolved = ServerNameResolver.TryResolve(null, new DefaultServerName("main"), out string serverName);

        resolved.Should().BeTrue();
        serverName.Should().Be("main");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_WithBlankRequest_FallsBackToDefault(string? requested)
    {
        bool resolved = ServerNameResolver.TryResolve(requested, new DefaultServerName("main"), out string serverName);

        resolved.Should().BeTrue();
        serverName.Should().Be("main");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_WithBlankRequestAndBlankDefault_ReturnsFalseWithEmpty(string? defaultValue)
    {
        bool resolved = ServerNameResolver.TryResolve(null, new DefaultServerName(defaultValue), out string serverName);

        resolved.Should().BeFalse();
        serverName.Should().BeEmpty();
    }
}
