using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Inkwell;

internal sealed class FileStorageOptionsValidator : IValidateOptions<FileStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, FileStorageOptions options)
    {
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Fail(results.Select(r => r.ErrorMessage ?? "Invalid FileStorageOptions."));
        }

        if (options.UploadUrlTtlMinutes <= 0 || options.DownloadUrlTtlMinutes <= 0)
        {
            return ValidateOptionsResult.Fail("TTL values must be positive.");
        }

        return ValidateOptionsResult.Success;
    }
}
