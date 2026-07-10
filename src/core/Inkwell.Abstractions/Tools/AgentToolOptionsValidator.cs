// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class AgentToolOptionsValidator : IValidateOptions<AgentToolOptions>
{
    public ValidateOptionsResult Validate(string? name, AgentToolOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid AgentToolOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
