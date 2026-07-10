using Inkwell;

namespace Inkwell.Persistence.EFCore.Entities;

internal sealed class AgentToolEntity : IHasTimestamps
{
    public Guid Id { get; init; }

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public string ParametersJsonSchema { get; init; } = "{}";

    public DateTimeOffset CreatedTime { get; init; }

    public DateTimeOffset UpdatedTime { get; init; }
}
