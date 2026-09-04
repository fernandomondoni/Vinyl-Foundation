using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Identity;

public sealed class UserIdentityService(
    IUserRepository userRepository,
    IExternalIdentityProfileProvider externalIdentityProfileProvider,
    TimeProvider timeProvider) : IUserIdentityService
{
    public async Task<User> GetOrCreateAsync(
        AuthenticatedUserContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var externalProfile = await externalIdentityProfileProvider.GetAsync(
            context,
            cancellationToken);
        var profile = new UserProfile(
            externalProfile?.Email ?? context.Email,
            externalProfile?.DisplayName ?? context.DisplayName);

        var user = await userRepository.FindByExternalIdentityAsync(
            context.Issuer,
            context.Subject,
            cancellationToken);

        if (user is null)
        {
            user = new UserBuilder()
                .FromAuthenticatedContext(
                    context with
                    {
                        Email = profile.Email,
                        DisplayName = profile.DisplayName
                    },
                    timeProvider.GetUtcNow())
                .Build();
        }
        else
        {
            user.SynchronizeAuthenticationProfile(
                profile,
                timeProvider.GetUtcNow());
        }

        await userRepository.SaveAsync(user, cancellationToken);
        return user;
    }
}
