namespace Vinyl.Identity.Application.Abstractions;

public interface IExternalIdentityProfileProvider
{
    Task<ExternalUserProfile?> GetAsync(
        AuthenticatedUserContext context,
        CancellationToken cancellationToken);
}
