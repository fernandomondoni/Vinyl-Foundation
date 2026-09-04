using System.Text.Json.Serialization;

namespace Vinyl.Identity.Application.Domain;

public enum ContentAccessLevel
{
    Free,
    Premium
}

public enum SubscriptionPlan
{
    Free,
    Premium
}

public enum SubscriptionStatus
{
    None,
    Active,
    Cancelled,
    Expired
}

public enum SubscriptionEventType
{
    Activated,
    Renewed,
    Cancelled,
    Expired
}

public sealed record SubscriptionEvent(
    string Provider,
    string EventId,
    Guid UserId,
    string ProviderSubscriptionId,
    SubscriptionEventType Type,
    DateTimeOffset OccurredAt,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? ExpiresAt = null);

public enum SubscriptionOperationStatus
{
    Applied,
    AlreadyProcessed,
    IgnoredOutOfOrder,
    UserNotFound,
    SubscriptionNotFound,
    InvalidEvent
}

public sealed record SubscriptionOperationResult(
    SubscriptionOperationStatus Status,
    UserSubscription? Subscription);

public sealed class SubscriptionEventReceipt
{
    [JsonConstructor]
    private SubscriptionEventReceipt()
    {
    }

    private SubscriptionEventReceipt(
        string id,
        string provider,
        string eventId,
        Guid userId,
        DateTimeOffset processedAt)
    {
        Id = id;
        Provider = provider;
        EventId = eventId;
        UserId = userId;
        ProcessedAt = processedAt;
    }

    [JsonInclude]
    public string Id { get; private set; } = string.Empty;

    [JsonInclude]
    public string Provider { get; private set; } = string.Empty;

    [JsonInclude]
    public string EventId { get; private set; } = string.Empty;

    [JsonInclude]
    public Guid UserId { get; private set; }

    [JsonInclude]
    public DateTimeOffset ProcessedAt { get; private set; }

    public static SubscriptionEventReceipt Create(
        string provider,
        string eventId,
        Guid userId,
        DateTimeOffset processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id must be a non-empty GUID.", nameof(userId));
        }

        return new SubscriptionEventReceipt(
            BuildId(provider, eventId),
            provider,
            eventId,
            userId,
            processedAt);
    }

    public static string BuildId(string provider, string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return $"{provider.Trim().ToLowerInvariant()}:{eventId.Trim()}";
    }
}

public sealed class UserSubscription
{
    [JsonConstructor]
    private UserSubscription()
    {
    }

    private UserSubscription(
        Guid id,
        Guid userId,
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
        string? provider,
        string? providerSubscriptionId,
        DateTimeOffset now,
        DateTimeOffset? eventOccurredAt = null)
    {
        Id = id;
        UserId = userId;
        Plan = SubscriptionPlan.Premium;
        Status = SubscriptionStatus.Active;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        Provider = provider;
        ProviderSubscriptionId = providerSubscriptionId;
        LastEventOccurredAt = eventOccurredAt;
        CreatedAt = now;
        UpdatedAt = now;
    }

    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public Guid UserId { get; private set; }

    [JsonInclude]
    public SubscriptionPlan Plan { get; private set; } = SubscriptionPlan.Free;

    [JsonInclude]
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.None;

    [JsonInclude]
    public DateTimeOffset StartsAt { get; private set; }

    [JsonInclude]
    public DateTimeOffset? ExpiresAt { get; private set; }

    [JsonInclude]
    public string? Provider { get; private set; }

    [JsonInclude]
    public string? ProviderSubscriptionId { get; private set; }

    [JsonInclude]
    public DateTimeOffset? LastEventOccurredAt { get; private set; }

    [JsonInclude]
    public DateTimeOffset CreatedAt { get; private set; }

    [JsonInclude]
    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserSubscription ActivatePremium(
        Guid id,
        Guid userId,
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset now,
        string? provider = null,
        string? providerSubscriptionId = null,
        DateTimeOffset? eventOccurredAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Subscription id must be a non-empty GUID.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id must be a non-empty GUID.", nameof(userId));
        }

        if (expiresAt is not null && expiresAt <= startsAt)
        {
            throw new ArgumentException(
                "Subscription expiration must be after its start date.",
                nameof(expiresAt));
        }

        return new UserSubscription(
            id,
            userId,
            startsAt,
            expiresAt,
            provider,
            providerSubscriptionId,
            now,
            eventOccurredAt);
    }

    public void Cancel(DateTimeOffset now)
    {
        Cancel(now, now);
    }

    public void Cancel(DateTimeOffset now, DateTimeOffset eventOccurredAt)
    {
        Status = SubscriptionStatus.Cancelled;
        LastEventOccurredAt = eventOccurredAt;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        Expire(now, now);
    }

    public void Expire(DateTimeOffset now, DateTimeOffset eventOccurredAt)
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = now;
        LastEventOccurredAt = eventOccurredAt;
    }

    public void ActivateOrRenew(
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
        string provider,
        string providerSubscriptionId,
        DateTimeOffset eventOccurredAt,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubscriptionId);

        if (expiresAt is not null && expiresAt <= startsAt)
        {
            throw new ArgumentException(
                "Subscription expiration must be after its start date.",
                nameof(expiresAt));
        }

        Plan = SubscriptionPlan.Premium;
        Status = SubscriptionStatus.Active;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        Provider = provider;
        ProviderSubscriptionId = providerSubscriptionId;
        LastEventOccurredAt = eventOccurredAt;
        UpdatedAt = now;
    }

    public bool GrantsPremiumAccess(DateTimeOffset now)
    {
        return Plan == SubscriptionPlan.Premium
            && Status == SubscriptionStatus.Active
            && StartsAt <= now
            && (ExpiresAt is null || ExpiresAt > now);
    }

    public SubscriptionStatus GetEffectiveStatus(DateTimeOffset now)
    {
        return Status == SubscriptionStatus.Active
            && ExpiresAt is not null
            && ExpiresAt <= now
            ? SubscriptionStatus.Expired
            : Status;
    }
}

public sealed record UserContentAccess(
    SubscriptionPlan EffectivePlan,
    SubscriptionStatus SubscriptionStatus,
    DateTimeOffset? SubscriptionExpiresAt,
    bool CanAccessPremiumContent)
{
    public bool CanAccess(ContentAccessLevel accessLevel)
    {
        return accessLevel == ContentAccessLevel.Free || CanAccessPremiumContent;
    }
}
