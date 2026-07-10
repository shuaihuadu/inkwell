using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

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
