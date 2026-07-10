using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentOptionsValidator : IValidateOptions<AgentOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
