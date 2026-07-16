// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

internal sealed class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid CacheOptions."));
        }

        if (options.MinTtlSeconds > options.MaxTtlSeconds)
        {
            return ValidateOptionsResult.Fail($"{nameof(options.MinTtlSeconds)} must be <= {nameof(options.MaxTtlSeconds)}.");
        }

        return ValidateOptionsResult.Success;
    }
}
