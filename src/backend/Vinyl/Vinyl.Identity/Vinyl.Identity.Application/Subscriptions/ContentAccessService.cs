using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Subscriptions;

public sealed class ContentAccessService(
    IUserSubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider) : IContentAccessService
{
    public async Task<UserContentAccess> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id must be a non-empty GUID.", nameof(userId));
        }

        var subscription = await subscriptionRepository.FindByUserIdAsync(
            userId,
            cancellationToken);
        if (subscription is null)
        {
            return new UserContentAccess(
                SubscriptionPlan.Free,
                SubscriptionStatus.None,
                null,
                false);
        }

        var now = timeProvider.GetUtcNow();
        var canAccessPremiumContent = subscription.GrantsPremiumAccess(now);

        return new UserContentAccess(
            canAccessPremiumContent
                ? SubscriptionPlan.Premium
                : SubscriptionPlan.Free,
            subscription.GetEffectiveStatus(now),
            subscription.ExpiresAt,
            canAccessPremiumContent);
    }

    public async Task<bool> CanAccessAsync(
        Guid userId,
        ContentAccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        if (accessLevel == ContentAccessLevel.Free)
        {
            return true;
        }

        var access = await GetForUserAsync(userId, cancellationToken);
        return access.CanAccessPremiumContent;
    }
}
