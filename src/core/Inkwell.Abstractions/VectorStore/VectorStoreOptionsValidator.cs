using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class VectorStoreOptionsValidator : IValidateOptions<VectorStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, VectorStoreOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid VectorStoreOptions."));
        }

        return ValidateOptionsResult.Success;
    }
}
