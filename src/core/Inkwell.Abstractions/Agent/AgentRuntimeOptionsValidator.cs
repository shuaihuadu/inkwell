// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class AgentRuntimeOptionsValidator : IValidateOptions<AgentRunOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentRunOptions options)
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
