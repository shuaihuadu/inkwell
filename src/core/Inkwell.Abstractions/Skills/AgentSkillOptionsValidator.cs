using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class AgentSkillOptionsValidator : IValidateOptions<AgentSkillOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentSkillOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentSkillOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
