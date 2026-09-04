using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<User?> FindByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken);

    Task SaveAsync(User user, CancellationToken cancellationToken);
}
