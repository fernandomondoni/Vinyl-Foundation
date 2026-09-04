using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Subscriptions;

public sealed class SubscriptionService(
    IUserRepository userRepository,
    IUserSubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<SubscriptionOperationResult> ApplyEventAsync(
        SubscriptionEvent subscriptionEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscriptionEvent);

        if (!IsValid(subscriptionEvent))
        {
            return new SubscriptionOperationResult(
                SubscriptionOperationStatus.InvalidEvent,
                null);
        }

        if (await subscriptionRepository.HasProcessedEventAsync(
                subscriptionEvent.Provider,
                subscriptionEvent.EventId,
                cancellationToken))
        {
            return new SubscriptionOperationResult(
                SubscriptionOperationStatus.AlreadyProcessed,
                null);
        }

        var user = await userRepository.FindByIdAsync(
            subscriptionEvent.UserId,
            cancellationToken);
        if (user is null)
        {
            return new SubscriptionOperationResult(
                SubscriptionOperationStatus.UserNotFound,
                null);
        }

        var currentSubscription = await subscriptionRepository.FindByUserIdAsync(
            subscriptionEvent.UserId,
            cancellationToken);

        if (!MatchesCurrentProviderSubscription(currentSubscription, subscriptionEvent))
        {
            return new SubscriptionOperationResult(
                SubscriptionOperationStatus.InvalidEvent,
                currentSubscription);
        }

        var now = timeProvider.GetUtcNow();
        var receipt = SubscriptionEventReceipt.Create(
            subscriptionEvent.Provider,
            subscriptionEvent.EventId,
            subscriptionEvent.UserId,
            now);

        if (currentSubscription?.LastEventOccurredAt is not null
            && subscriptionEvent.OccurredAt <= currentSubscription.LastEventOccurredAt)
        {
            await subscriptionRepository.SaveEventReceiptAsync(
                receipt,
                cancellationToken);

            return new SubscriptionOperationResult(
                SubscriptionOperationStatus.IgnoredOutOfOrder,
                currentSubscription);
        }

        switch (subscriptionEvent.Type)
        {
            case SubscriptionEventType.Activated:
            case SubscriptionEventType.Renewed:
                currentSubscription = ApplyActivationOrRenewal(
                    currentSubscription,
                    subscriptionEvent,
                    now);
                break;
            case SubscriptionEventType.Cancelled:
                if (currentSubscription is null)
                {
                    return new SubscriptionOperationResult(
                        SubscriptionOperationStatus.SubscriptionNotFound,
                        null);
                }

                currentSubscription.Cancel(now, subscriptionEvent.OccurredAt);
                break;
            case SubscriptionEventType.Expired:
                if (currentSubscription is null)
                {
                    return new SubscriptionOperationResult(
                        SubscriptionOperationStatus.SubscriptionNotFound,
                        null);
                }

                currentSubscription.Expire(now, subscriptionEvent.OccurredAt);
                break;
            default:
                return new SubscriptionOperationResult(
                    SubscriptionOperationStatus.InvalidEvent,
                    currentSubscription);
        }

        await subscriptionRepository.SaveWithEventReceiptAsync(
            currentSubscription,
            receipt,
            cancellationToken);

        return new SubscriptionOperationResult(
            SubscriptionOperationStatus.Applied,
            currentSubscription);
    }

    private static UserSubscription ApplyActivationOrRenewal(
        UserSubscription? currentSubscription,
        SubscriptionEvent subscriptionEvent,
        DateTimeOffset now)
    {
        var startsAt = subscriptionEvent.StartsAt ?? subscriptionEvent.OccurredAt;
        if (currentSubscription is null)
        {
            return UserSubscription.ActivatePremium(
                Guid.NewGuid(),
                subscriptionEvent.UserId,
                startsAt,
                subscriptionEvent.ExpiresAt,
                now,
                subscriptionEvent.Provider,
                subscriptionEvent.ProviderSubscriptionId,
                subscriptionEvent.OccurredAt);
        }

        currentSubscription.ActivateOrRenew(
            startsAt,
            subscriptionEvent.ExpiresAt,
            subscriptionEvent.Provider,
            subscriptionEvent.ProviderSubscriptionId,
            subscriptionEvent.OccurredAt,
            now);
        return currentSubscription;
    }

    private static bool IsValid(SubscriptionEvent subscriptionEvent)
    {
        return !string.IsNullOrWhiteSpace(subscriptionEvent.Provider)
            && !string.IsNullOrWhiteSpace(subscriptionEvent.EventId)
            && !string.IsNullOrWhiteSpace(subscriptionEvent.ProviderSubscriptionId)
            && subscriptionEvent.UserId != Guid.Empty
            && subscriptionEvent.OccurredAt != default
            && Enum.IsDefined(subscriptionEvent.Type)
            && IsValidPeriod(subscriptionEvent);
    }

    private static bool IsValidPeriod(SubscriptionEvent subscriptionEvent)
    {
        if (subscriptionEvent.Type is not (
            SubscriptionEventType.Activated or SubscriptionEventType.Renewed))
        {
            return true;
        }

        var startsAt = subscriptionEvent.StartsAt ?? subscriptionEvent.OccurredAt;
        return subscriptionEvent.ExpiresAt is null
            || subscriptionEvent.ExpiresAt > startsAt;
    }

    private static bool MatchesCurrentProviderSubscription(
        UserSubscription? currentSubscription,
        SubscriptionEvent subscriptionEvent)
    {
        if (currentSubscription is null)
        {
            return subscriptionEvent.Type is
                SubscriptionEventType.Activated or SubscriptionEventType.Renewed;
        }

        if (currentSubscription.Provider is null
            || currentSubscription.ProviderSubscriptionId is null)
        {
            return true;
        }

        if (subscriptionEvent.Type is SubscriptionEventType.Cancelled
            or SubscriptionEventType.Expired)
        {
            return string.Equals(
                       currentSubscription.Provider,
                       subscriptionEvent.Provider,
                       StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                       currentSubscription.ProviderSubscriptionId,
                       subscriptionEvent.ProviderSubscriptionId,
                       StringComparison.Ordinal);
        }

        return currentSubscription.Status is SubscriptionStatus.Cancelled
                or SubscriptionStatus.Expired
            || (string.Equals(
                    currentSubscription.Provider,
                    subscriptionEvent.Provider,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    currentSubscription.ProviderSubscriptionId,
                    subscriptionEvent.ProviderSubscriptionId,
                    StringComparison.Ordinal));
    }
}
