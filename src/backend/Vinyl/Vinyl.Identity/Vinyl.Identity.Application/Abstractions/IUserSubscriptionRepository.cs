using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> FindByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> HasProcessedEventAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        UserSubscription subscription,
        CancellationToken cancellationToken);

    Task SaveEventReceiptAsync(
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken);

    Task SaveWithEventReceiptAsync(
        UserSubscription? subscription,
        SubscriptionEventReceipt receipt,
        CancellationToken cancellationToken);
}
