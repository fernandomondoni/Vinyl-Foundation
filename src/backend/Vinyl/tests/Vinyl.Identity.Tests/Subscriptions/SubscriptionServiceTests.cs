using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;
using Vinyl.Identity.Application.Subscriptions;

namespace Vinyl.Identity.Tests.Subscriptions;

public sealed class SubscriptionServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActivationEventCreatesPremiumSubscriptionForAnExistingUser()
    {
        var user = CreateUser();
        var repository = new InMemoryRepository(user);
        var service = CreateService(repository);

        var result = await service.ApplyEventAsync(
            CreateEvent(user.Id, "activation-1", SubscriptionEventType.Activated),
            CancellationToken.None);

        Assert.Equal(SubscriptionOperationStatus.Applied, result.Status);
        Assert.NotNull(result.Subscription);
        Assert.Equal(SubscriptionPlan.Premium, result.Subscription.Plan);
        Assert.Equal(SubscriptionStatus.Active, result.Subscription.Status);
        Assert.Equal("provider-subscription-1", result.Subscription.ProviderSubscriptionId);
    }

    [Fact]
    public async Task RepeatedEventIsAppliedOnlyOnce()
    {
        var user = CreateUser();
        var repository = new InMemoryRepository(user);
        var service = CreateService(repository);
        var subscriptionEvent = CreateEvent(
            user.Id,
            "activation-duplicate",
            SubscriptionEventType.Activated);

        var firstResult = await service.ApplyEventAsync(
            subscriptionEvent,
            CancellationToken.None);
        var secondResult = await service.ApplyEventAsync(
            subscriptionEvent,
            CancellationToken.None);

        Assert.Equal(SubscriptionOperationStatus.Applied, firstResult.Status);
        Assert.Equal(SubscriptionOperationStatus.AlreadyProcessed, secondResult.Status);
        Assert.Single(repository.EventReceipts);
    }

    [Fact]
    public async Task CancellationEventRemovesPremiumAccess()
    {
        var user = CreateUser();
        var repository = new InMemoryRepository(user);
        var service = CreateService(repository);
        var accessService = new ContentAccessService(repository, new FixedTimeProvider(Now));

        await service.ApplyEventAsync(
            CreateEvent(user.Id, "activation-2", SubscriptionEventType.Activated),
            CancellationToken.None);
        var activeAccess = await accessService.GetForUserAsync(
            user.Id,
            CancellationToken.None);

        var result = await service.ApplyEventAsync(
            CreateEvent(
                user.Id,
                "cancellation-1",
                SubscriptionEventType.Cancelled,
                Now.AddMinutes(-1)),
            CancellationToken.None);
        var cancelledAccess = await accessService.GetForUserAsync(
            user.Id,
            CancellationToken.None);

        Assert.Equal(SubscriptionOperationStatus.Applied, result.Status);
        Assert.True(activeAccess.CanAccessPremiumContent);
        Assert.False(cancelledAccess.CanAccessPremiumContent);
        Assert.Equal(SubscriptionStatus.Cancelled, cancelledAccess.SubscriptionStatus);
    }

    [Fact]
    public async Task OlderEventDoesNotOverwriteNewerSubscriptionState()
    {
        var user = CreateUser();
        var repository = new InMemoryRepository(user);
        var service = CreateService(repository);

        await service.ApplyEventAsync(
            CreateEvent(
                user.Id,
                "activation-3",
                SubscriptionEventType.Activated,
                Now.AddMinutes(-1),
                Now.AddDays(30)),
            CancellationToken.None);

        var result = await service.ApplyEventAsync(
            CreateEvent(
                user.Id,
                "renewal-old",
                SubscriptionEventType.Renewed,
                Now.AddMinutes(-2),
                Now.AddDays(60)),
            CancellationToken.None);

        Assert.Equal(SubscriptionOperationStatus.IgnoredOutOfOrder, result.Status);
        Assert.Equal(Now.AddDays(30), repository.Subscription?.ExpiresAt);
        Assert.Equal(2, repository.EventReceipts.Count);
    }

    [Fact]
    public async Task EventForUnknownUserIsRejectedWithoutCreatingSubscription()
    {
        var repository = new InMemoryRepository(null);
        var service = CreateService(repository);
        var unknownUserId = Guid.NewGuid();

        var result = await service.ApplyEventAsync(
            CreateEvent(unknownUserId, "activation-unknown", SubscriptionEventType.Activated),
            CancellationToken.None);

        Assert.Equal(SubscriptionOperationStatus.UserNotFound, result.Status);
        Assert.Null(repository.Subscription);
        Assert.Empty(repository.EventReceipts);
    }

    private static SubscriptionService CreateService(InMemoryRepository repository)
    {
        return new SubscriptionService(
            repository,
            repository,
            new FixedTimeProvider(Now));
    }

    private static SubscriptionEvent CreateEvent(
        Guid userId,
        string eventId,
        SubscriptionEventType type,
        DateTimeOffset? occurredAt = null,
        DateTimeOffset? expiresAt = null)
    {
        return new SubscriptionEvent(
            "provider",
            eventId,
            userId,
            "provider-subscription-1",
            type,
            occurredAt ?? Now.AddMinutes(-2),
            Now.AddMinutes(-2),
            expiresAt ?? Now.AddDays(30));
    }

    private static User CreateUser()
    {
        return User.Register(
            Guid.NewGuid(),
            new ExternalIdentity("https://issuer.example", "subject", "oidc"),
            new UserProfile("user@example.com", "Vinyl User"),
            Now.AddDays(-1));
    }

    private sealed class InMemoryRepository(User? user) :
        IUserRepository,
        IUserSubscriptionRepository
    {
        public User? User { get; } = user;

        public UserSubscription? Subscription { get; private set; }

        public Dictionary<string, SubscriptionEventReceipt> EventReceipts { get; } = [];

        public Task<User?> FindByIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<User?>(User?.Id == userId ? User : null);
        }

        public Task<User?> FindByExternalIdentityAsync(
            string issuer,
            string subject,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = User?.ExternalIdentities.Any(identity =>
                identity.Issuer == issuer && identity.Subject == subject) == true;
            return Task.FromResult<User?>(matches ? User : null);
        }

        public Task SaveAsync(User userToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<UserSubscription?> FindByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<UserSubscription?>(
                Subscription?.UserId == userId ? Subscription : null);
        }

        public Task<bool> HasProcessedEventAsync(
            string provider,
            string eventId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(EventReceipts.ContainsKey(
                SubscriptionEventReceipt.BuildId(provider, eventId)));
        }

        public Task SaveAsync(
            UserSubscription subscription,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Subscription = subscription;
            return Task.CompletedTask;
        }

        public Task SaveEventReceiptAsync(
            SubscriptionEventReceipt receipt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EventReceipts[receipt.Id] = receipt;
            return Task.CompletedTask;
        }

        public Task SaveWithEventReceiptAsync(
            UserSubscription? subscription,
            SubscriptionEventReceipt receipt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Subscription = subscription;
            EventReceipts[receipt.Id] = receipt;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
