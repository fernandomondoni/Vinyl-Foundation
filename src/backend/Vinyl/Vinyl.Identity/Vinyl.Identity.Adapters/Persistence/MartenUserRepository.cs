using Marten;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class MartenUserRepository(IDocumentSession session) : IUserRepository
{
    public Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return session.LoadAsync<User>(userId, cancellationToken);
    }

    public Task<User?> FindByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken)
    {
        return session.Query<User>()
            .Where(user => user.ExternalIdentities.Any(identity =>
                identity.Issuer == issuer && identity.Subject == subject))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        session.Store(user);
        await session.SaveChangesAsync(cancellationToken);
    }
}
