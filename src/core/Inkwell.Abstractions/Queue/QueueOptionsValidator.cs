// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class QueueOptionsValidator : IValidateOptions<QueueOptions>
{
    public ValidateOptionsResult Validate(string? name, QueueOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid QueueOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
