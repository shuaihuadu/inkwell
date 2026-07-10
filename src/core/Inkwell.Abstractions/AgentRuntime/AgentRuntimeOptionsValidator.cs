using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentRuntimeOptionsValidator : IValidateOptions<AgentRuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentRuntimeOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentRuntimeOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
