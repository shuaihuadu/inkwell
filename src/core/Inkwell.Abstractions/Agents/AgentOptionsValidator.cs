using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentOptionsValidator : IValidateOptions<AgentOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
