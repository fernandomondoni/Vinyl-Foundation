using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface ISubscriptionService
{
    Task<SubscriptionOperationResult> ApplyEventAsync(
        SubscriptionEvent subscriptionEvent,
        CancellationToken cancellationToken);
}
