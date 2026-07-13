// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class LiteLLMModelRegistryOptionsValidator : IValidateOptions<LiteLLMModelRegistryOptions>
{
    public ValidateOptionsResult Validate(string? name, LiteLLMModelRegistryOptions options)
    {
        List<ValidationResult> optionResults = [];
        ValidationContext optionContext = new(options);
        List<string> errors = [];

        if (!Validator.TryValidateObject(options, optionContext, optionResults, validateAllProperties: true))
        {
            errors.AddRange(optionResults.Select(result => result.ErrorMessage ?? "Invalid LiteLLMModelRegistryOptions."));
        }

        foreach (LiteLLMModelMetadataOptions model in options.Models)
        {
            List<ValidationResult> modelResults = [];
            ValidationContext modelContext = new(model);

            if (!Validator.TryValidateObject(model, modelContext, modelResults, validateAllProperties: true))
            {
                errors.AddRange(modelResults.Select(result => result.ErrorMessage ?? $"Invalid LiteLLM model metadata '{model.Id}'."));
            }
        }

        List<string> duplicateIds = options.Models
            .GroupBy(model => model.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            errors.Add($"Duplicate LiteLLM model metadata Id(s): {string.Join(", ", duplicateIds)}.");
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}