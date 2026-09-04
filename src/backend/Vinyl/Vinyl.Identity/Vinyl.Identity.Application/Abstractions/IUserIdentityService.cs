using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IUserIdentityService
{
    Task<User> GetOrCreateAsync(
        AuthenticatedUserContext context,
        CancellationToken cancellationToken);
}
