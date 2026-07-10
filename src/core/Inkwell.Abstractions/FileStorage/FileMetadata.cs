namespace Inkwell;

public sealed record class FileMetadata(string ContentType, IReadOnlyDictionary<string, string>? CustomMetadata = null, string? ContentDisposition = null);
