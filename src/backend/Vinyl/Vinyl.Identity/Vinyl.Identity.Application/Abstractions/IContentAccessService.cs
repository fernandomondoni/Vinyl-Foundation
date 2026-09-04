using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IContentAccessService
{
    Task<UserContentAccess> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> CanAccessAsync(
        Guid userId,
        ContentAccessLevel accessLevel,
        CancellationToken cancellationToken);
}
