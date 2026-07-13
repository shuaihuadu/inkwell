// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class ConfigurationModelRegistryOptionsValidator : IValidateOptions<ConfigurationModelRegistryOptions>
{
    public ValidateOptionsResult Validate(string? name, ConfigurationModelRegistryOptions options)
    {
        List<ValidationResult> results = [];
        ValidationContext context = new(options);

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid ConfigurationModelRegistryOptions."));
        }

        List<string> errors = [];

        foreach (ConfigurationModelEntryOptions entry in options.Models)
        {
            ValidationContext entryContext = new(entry);
            List<ValidationResult> entryResults = [];

            if (!Validator.TryValidateObject(entry, entryContext, entryResults, validateAllProperties: true))
            {
                errors.AddRange(entryResults.Select(r => r.ErrorMessage ?? $"Invalid model entry '{entry.Id}'."));
            }
        }

        List<string> duplicateIds = options.Models
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            errors.Add($"Duplicate model Id(s): {string.Join(", ", duplicateIds)}.");
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
