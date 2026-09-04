using System.Collections.Concurrent;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> users = new(StringComparer.Ordinal);

    public Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = users.Values.FirstOrDefault(candidate => candidate.Id == userId);
        return Task.FromResult(user);
    }

    public Task<User?> FindByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        users.TryGetValue(BuildKey(issuer, subject), out var user);
        return Task.FromResult(user);
    }

    public Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var externalIdentity in user.ExternalIdentities)
        {
            users[BuildKey(externalIdentity.Issuer, externalIdentity.Subject)] = user;
        }

        return Task.CompletedTask;
    }

    private static string BuildKey(string issuer, string subject) => $"{issuer}|{subject}";
}
