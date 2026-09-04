using System.Collections.Concurrent;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class InMemoryUserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly ConcurrentDictionary<Guid, UserSubscription> subscriptions = new();
    private readonly ConcurrentDictionary<string, SubscriptionEventReceipt> eventReceipts = new();

    public Task<UserSubscription?> FindByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        subscriptions.TryGetValue(userId, out var subscription);
        return Task.FromResult(subscription);
    }

    public Task<bool> HasProcessedEventAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(eventReceipts.ContainsKey(
            SubscriptionEventReceipt.BuildId(provider, eventId)));
    }

    public Task SaveAsync(
        UserSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        cancellationToken.ThrowIfCancellationRequested();
        subscriptions[subscription.UserId] = subscription;
        return Task.CompletedTask;
    }

    public Task SaveEventReceiptAsync(
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        cancellationToken.ThrowIfCancellationRequested();
        eventReceipts[receipt.Id] = receipt;
        return Task.CompletedTask;
    }

    public Task SaveWithEventReceiptAsync(
        UserSubscription? subscription,
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        cancellationToken.ThrowIfCancellationRequested();

        if (subscription is not null)
        {
            subscriptions[subscription.UserId] = subscription;
        }

        eventReceipts[receipt.Id] = receipt;
        return Task.CompletedTask;
    }
}
