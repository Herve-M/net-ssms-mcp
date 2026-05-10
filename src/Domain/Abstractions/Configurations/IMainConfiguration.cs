using ssmsmcp.Domain.Configurations;

namespace ssmsmcp.Domain.Abstractions.Configurations;

public interface IMainConfiguration
{
    public DataSource[] DataSources { get; }
}
