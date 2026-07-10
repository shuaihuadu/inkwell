using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentConversationOptionsValidator : IValidateOptions<AgentConversationOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentConversationOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentConversationOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
