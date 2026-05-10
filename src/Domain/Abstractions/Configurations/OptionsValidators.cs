using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ssmsmcp.Domain.Abstractions.Configurations;

public sealed class OptionsValidators<TOptions>(IServiceProvider serviceProvider)
    : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using IServiceScope scopeServiceProdiver = serviceProvider.CreateScope();

        IValidator<TOptions> optionValidator =
            scopeServiceProdiver.ServiceProvider.GetRequiredService<IValidator<TOptions>>();

        ValidationResult result = optionValidator.Validate(options);
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> errors = new(result.Errors.Count);
        foreach (ValidationFailure failure in result.Errors)
        {
            errors.Add($"{failure.PropertyName} failed: {failure.ErrorMessage}");
        }

        return ValidateOptionsResult.Fail(errors);
    }
}
