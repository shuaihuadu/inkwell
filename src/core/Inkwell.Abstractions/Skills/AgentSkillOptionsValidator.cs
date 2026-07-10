// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class AgentSkillOptionsValidator : IValidateOptions<AgentSkillOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentSkillOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentSkillOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
