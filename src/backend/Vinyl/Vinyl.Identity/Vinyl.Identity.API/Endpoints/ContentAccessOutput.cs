using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.API.Endpoints;

public sealed record ContentAccessOutput(
    string Plan,
    string SubscriptionStatus,
    bool CanAccessPremiumContent,
    DateTimeOffset? SubscriptionExpiresAt)
{
    public static ContentAccessOutput From(UserContentAccess access)
    {
        return new ContentAccessOutput(
            access.EffectivePlan.ToString(),
            access.SubscriptionStatus.ToString(),
            access.CanAccessPremiumContent,
            access.SubscriptionExpiresAt);
    }
}
