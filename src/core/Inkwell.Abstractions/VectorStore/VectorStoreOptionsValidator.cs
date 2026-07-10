// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class VectorStoreOptionsValidator : IValidateOptions<VectorStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, VectorStoreOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid VectorStoreOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
