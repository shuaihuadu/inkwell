// Copyright (c) ShuaiHua Du. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Inkwell;

internal sealed class FileStorageOptionsValidator : IValidateOptions<FileStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, FileStorageOptions options)
    {
        ValidationContext context = new(options);
        List<ValidationResult> results = [];

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
