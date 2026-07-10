using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentToolOptionsValidator : IValidateOptions<AgentToolOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentToolOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentToolOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
