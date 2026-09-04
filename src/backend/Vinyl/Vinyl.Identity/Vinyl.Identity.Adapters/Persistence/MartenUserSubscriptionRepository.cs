using Marten;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class MartenUserSubscriptionRepository(IDocumentSession session)
    : IUserSubscriptionRepository
{
    public Task<UserSubscription?> FindByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return session.Query<UserSubscription>()
            .Where(subscription => subscription.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasProcessedEventAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken)
    {
        var receipt = await session.LoadAsync<SubscriptionEventReceipt>(
            SubscriptionEventReceipt.BuildId(provider, eventId),
            cancellationToken);
        return receipt is not null;
    }

    public async Task SaveAsync(
        UserSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        session.Store(subscription);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveEventReceiptAsync(
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        session.Store(receipt);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveWithEventReceiptAsync(
        UserSubscription? subscription,
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (subscription is not null)
        {
            session.Store(subscription);
        }

        session.Store(receipt);
        await session.SaveChangesAsync(cancellationToken);
    }
}
