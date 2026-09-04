using System.Text.Json.Serialization;

namespace Vinyl.Identity.Application.Domain;

public sealed class User
{
    [JsonConstructor]
    private User()
    {
    }

    private User(
        Guid id,
        ExternalIdentity externalIdentity,
        UserProfile profile,
        DateTimeOffset now)
    {
        Id = id;
        ExternalIdentities = [externalIdentity];
        Profile = profile;
        Status = UserStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public List<ExternalIdentity> ExternalIdentities { get; private set; } = [];

    [JsonInclude]
    public UserProfile Profile { get; private set; } = new(null, null);

    [JsonInclude]
    public UserStatus Status { get; private set; }

    [JsonInclude]
    public DateTimeOffset CreatedAt { get; private set; }

    [JsonInclude]
    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Register(
        Guid id,
        ExternalIdentity externalIdentity,
        UserProfile profile,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalIdentity.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalIdentity.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalIdentity.Provider);

        return new User(id, externalIdentity, profile, now);
    }

    public void SynchronizeAuthenticationProfile(UserProfile profile, DateTimeOffset now)
    {
        Profile = profile;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = UserStatus.Suspended;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        Status = UserStatus.Active;
        UpdatedAt = now;
    }
}
