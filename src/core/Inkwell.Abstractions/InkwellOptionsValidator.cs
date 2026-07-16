// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

internal sealed class InkwellOptionsValidator : IValidateOptions<InkwellOptions>
{
    private static readonly string[] validEnvironments = ["dev", "staging", "prod"];

    public ValidateOptionsResult Validate(string? name, InkwellOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid InkwellOptions."));
        }

        if (!validEnvironments.Contains(options.Environment, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail($"{nameof(options.Environment)} must be one of: {string.Join(", ", validEnvironments)}.");
        }

        return ValidateOptionsResult.Success;
    }
}
