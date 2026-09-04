using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;
using Vinyl.Identity.Application.Subscriptions;

namespace Vinyl.Identity.Tests.Subscriptions;

public sealed class ContentAccessServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UserWithoutSubscriptionCanAccessFreeContentOnly()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var access = await service.GetForUserAsync(userId, CancellationToken.None);

        Assert.Equal(SubscriptionPlan.Free, access.EffectivePlan);
        Assert.Equal(SubscriptionStatus.None, access.SubscriptionStatus);
        Assert.True(access.CanAccess(ContentAccessLevel.Free));
        Assert.False(access.CanAccess(ContentAccessLevel.Premium));
    }

    [Fact]
    public async Task ActivePremiumSubscriptionGrantsPremiumContentAccess()
    {
        var userId = Guid.NewGuid();
        var subscription = UserSubscription.ActivatePremium(
            Guid.NewGuid(),
            userId,
            Now.AddDays(-1),
            Now.AddDays(30),
            Now,
            "test",
            "subscription-123");
        var service = CreateService(subscription);

        var access = await service.GetForUserAsync(userId, CancellationToken.None);

        Assert.Equal(SubscriptionPlan.Premium, access.EffectivePlan);
        Assert.Equal(SubscriptionStatus.Active, access.SubscriptionStatus);
        Assert.True(access.CanAccess(ContentAccessLevel.Premium));
        Assert.True(await service.CanAccessAsync(
            userId,
            ContentAccessLevel.Premium,
            CancellationToken.None));
    }

    [Fact]
    public async Task ExpiredPremiumSubscriptionFallsBackToFreeAccess()
    {
        var userId = Guid.NewGuid();
        var subscription = UserSubscription.ActivatePremium(
            Guid.NewGuid(),
            userId,
            Now.AddDays(-30),
            Now.AddMinutes(-1),
            Now.AddDays(-30));
        var service = CreateService(subscription);

        var access = await service.GetForUserAsync(userId, CancellationToken.None);

        Assert.Equal(SubscriptionPlan.Free, access.EffectivePlan);
        Assert.Equal(SubscriptionStatus.Expired, access.SubscriptionStatus);
        Assert.False(access.CanAccess(ContentAccessLevel.Premium));
    }

    [Fact]
    public async Task CancelledPremiumSubscriptionDoesNotGrantPremiumContentAccess()
    {
        var userId = Guid.NewGuid();
        var subscription = UserSubscription.ActivatePremium(
            Guid.NewGuid(),
            userId,
            Now.AddDays(-1),
            Now.AddDays(30),
            Now.AddDays(-1));
        subscription.Cancel(Now);
        var service = CreateService(subscription);

        var access = await service.GetForUserAsync(userId, CancellationToken.None);

        Assert.Equal(SubscriptionPlan.Free, access.EffectivePlan);
        Assert.Equal(SubscriptionStatus.Cancelled, access.SubscriptionStatus);
        Assert.False(access.CanAccess(ContentAccessLevel.Premium));
    }

    [Fact]
    public void SubscriptionCanBeDeserializedFromMartenDocumentJson()
    {
        var userId = Guid.NewGuid();
        var subscription = UserSubscription.ActivatePremium(
            Guid.NewGuid(),
            userId,
            Now.AddDays(-1),
            Now.AddDays(30),
            Now);

        var restored = System.Text.Json.JsonSerializer.Deserialize<UserSubscription>(
            System.Text.Json.JsonSerializer.Serialize(subscription));

        Assert.NotNull(restored);
        Assert.Equal(subscription.Id, restored.Id);
        Assert.Equal(subscription.UserId, restored.UserId);
        Assert.Equal(subscription.Plan, restored.Plan);
        Assert.Equal(subscription.Status, restored.Status);
        Assert.Equal(subscription.StartsAt, restored.StartsAt);
        Assert.Equal(subscription.ExpiresAt, restored.ExpiresAt);
    }

    private static ContentAccessService CreateService(
        UserSubscription? subscription = null)
    {
        return new ContentAccessService(
            new InMemoryUserSubscriptionRepository(subscription),
            new FixedTimeProvider(Now));
    }

    private sealed class InMemoryUserSubscriptionRepository(
        UserSubscription? subscription) : IUserSubscriptionRepository
    {
        public Task<UserSubscription?> FindByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<UserSubscription?>(
                subscription?.UserId == userId ? subscription : null);
        }

        public Task<bool> HasProcessedEventAsync(
            string provider,
            string eventId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }

        public Task SaveAsync(
            UserSubscription subscriptionToSave,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SaveEventReceiptAsync(
            SubscriptionEventReceipt receipt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SaveWithEventReceiptAsync(
            UserSubscription? subscriptionToSave,
            SubscriptionEventReceipt receipt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
