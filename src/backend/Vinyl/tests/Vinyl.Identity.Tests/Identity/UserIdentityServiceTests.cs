using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;
using Vinyl.Identity.Application.Identity;

namespace Vinyl.Identity.Tests.Identity;

public sealed class UserIdentityServiceTests
{
    [Fact]
    public async Task GetOrCreateReturnsTheSameLocalUserForTheSameExternalSubject()
    {
        var repository = new InMemoryUserRepository();
        var service = new UserIdentityService(
            repository,
            new StubExternalIdentityProfileProvider(null),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero)));
        var firstContext = CreateContext("external-subject", "first@example.com");
        var secondContext = CreateContext("external-subject", "second@example.com");

        var firstUser = await service.GetOrCreateAsync(firstContext, CancellationToken.None);
        var secondUser = await service.GetOrCreateAsync(secondContext, CancellationToken.None);

        Assert.Equal(firstUser.Id, secondUser.Id);
        Assert.Equal("second@example.com", secondUser.Profile.Email);
    }

    [Fact]
    public async Task GetOrCreateUsesTheExternalProfileWhenTokenHasNoProfileClaims()
    {
        var repository = new InMemoryUserRepository();
        var service = new UserIdentityService(
            repository,
            new StubExternalIdentityProfileProvider(
                new ExternalUserProfile("profile@example.com", "Cognito User")),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero)));

        var user = await service.GetOrCreateAsync(
            CreateContext("external-subject", null),
            CancellationToken.None);

        Assert.Equal("profile@example.com", user.Profile.Email);
        Assert.Equal("Cognito User", user.Profile.DisplayName);
    }

    private static AuthenticatedUserContext CreateContext(string subject, string? email)
    {
        return new AuthenticatedUserContext(
            "https://issuer.example/identity",
            subject,
            "oidc",
            email,
            "Vinyl User",
            null,
            null,
            null,
            [],
            new Dictionary<string, string>());
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private User? user;

        public Task<User?> FindByIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(user?.Id == userId ? user : null);
        }

        public Task<User?> FindByExternalIdentityAsync(
            string issuer,
            string subject,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matchingUser = user?.ExternalIdentities.Any(identity =>
                identity.Issuer == issuer && identity.Subject == subject) == true
                ? user
                : null;
            return Task.FromResult(matchingUser);
        }

        public Task SaveAsync(User userToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            user = userToSave;
            return Task.CompletedTask;
        }
    }

    private sealed class StubExternalIdentityProfileProvider(ExternalUserProfile? profile)
        : IExternalIdentityProfileProvider
    {
        public Task<ExternalUserProfile?> GetAsync(
            AuthenticatedUserContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(profile);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
