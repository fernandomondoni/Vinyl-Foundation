using System.Text.Json.Serialization;

namespace Vinyl.Identity.Application.Domain;

public sealed class Workspace
{
    [JsonConstructor]
    private Workspace()
    {
    }

    private Workspace(Guid id, string name, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        CreatedAt = now;
    }

    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public DateTimeOffset CreatedAt { get; private set; }

    public static Workspace Create(Guid id, string name, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Workspace(id, name.Trim(), now);
    }
}
