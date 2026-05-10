namespace ssmsmcp.Infrastructure.Abstractions.Configurations;

public record OpenTelemetrySettings
{
    public OpenTelemetrySettings()
    {
    }

    internal bool WithAspNetCore { get; set; }
    public void EnableAspNetCore()
    {
        WithAspNetCore = true;
    }

    internal bool WithMcp { get; set; }
    public void EnableMcp()
    {
        WithMcp = true;
    }
}