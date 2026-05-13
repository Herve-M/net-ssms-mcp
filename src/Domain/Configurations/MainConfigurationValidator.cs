using System;
using FluentValidation;
using Microsoft.Data.SqlClient;

namespace ssmsmcp.Domain.Configurations;

public sealed class MainConfigurationValidator
    : AbstractValidator<MainConfiguration>
{
    public MainConfigurationValidator()
    {
        RuleFor(x => x)
            .NotNull();

        RuleForEach(x => x.DataSources)
            .NotEmpty()
            .WithMessage("Data sources must has at least one SQL server")
            .Must((ds) =>
            {
                try
                {
                    SqlConnectionStringBuilder _ = new(ds.ConnectionString);
                }
                catch (Exception _)
                {
                    return false;
                }

                return true;
            })
            .WithMessage("Invalid connection string format")
            ;
    }
}
