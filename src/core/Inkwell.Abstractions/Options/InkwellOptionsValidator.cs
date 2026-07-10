using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class InkwellOptionsValidator : IValidateOptions<InkwellOptions>
{
    private static readonly string[] ValidEnvironments = ["dev", "staging", "prod"];

    public ValidateOptionsResult Validate(string? name, InkwellOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid InkwellOptions."));
        }

        if (!ValidEnvironments.Contains(options.Environment, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail($"{nameof(options.Environment)} must be one of: {string.Join(", ", ValidEnvironments)}.");
        }

        return ValidateOptionsResult.Success;
    }
}
