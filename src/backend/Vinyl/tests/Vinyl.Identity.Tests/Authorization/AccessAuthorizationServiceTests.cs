using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Tests.Authorization;

public sealed class AccessAuthorizationServiceTests
{
    [Fact]
    public async Task OwnerCanReadAndManageTheWorkspace()
    {
        var user = CreateUser();
        var workspace = Workspace.Create(
            Guid.NewGuid(),
            "My Workspace",
            DateTimeOffset.UtcNow);
        var membership = Membership.Create(
            Guid.NewGuid(),
            user.Id,
            workspace.Id,
            DefaultRoles.OwnerId,
            DateTimeOffset.UtcNow);
        var userRepository = new InMemoryUserRepository(user);
        var workspaceRepository = new InMemoryWorkspaceRepository(workspace, membership);
        var service = new AccessAuthorizationService(userRepository, workspaceRepository);
        var context = CreateContext(user.ExternalIdentities.Single());

        var readDecision = await service.AuthorizeAsync(
            context,
            CreateRequest("read", workspace.Id),
            CancellationToken.None);
        var manageDecision = await service.AuthorizeAsync(
            context,
            CreateRequest("manage", workspace.Id),
            CancellationToken.None);

        Assert.True(readDecision.IsAllowed);
        Assert.True(manageDecision.IsAllowed);
    }

    [Fact]
    public async Task UserWithoutMembershipIsDenied()
    {
        var user = CreateUser();
        var workspace = Workspace.Create(
            Guid.NewGuid(),
            "My Workspace",
            DateTimeOffset.UtcNow);
        var userRepository = new InMemoryUserRepository(user);
        var workspaceRepository = new InMemoryWorkspaceRepository(workspace, null);
        var service = new AccessAuthorizationService(userRepository, workspaceRepository);

        var decision = await service.AuthorizeAsync(
            CreateContext(user.ExternalIdentities.Single()),
            CreateRequest("read", workspace.Id),
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public async Task MemberCannotManageTheWorkspace()
    {
        var user = CreateUser();
        var workspace = Workspace.Create(
            Guid.NewGuid(),
            "My Workspace",
            DateTimeOffset.UtcNow);
        var membership = Membership.Create(
            Guid.NewGuid(),
            user.Id,
            workspace.Id,
            DefaultRoles.MemberId,
            DateTimeOffset.UtcNow);
        var service = new AccessAuthorizationService(
            new InMemoryUserRepository(user),
            new InMemoryWorkspaceRepository(workspace, membership));

        var decision = await service.AuthorizeAsync(
            CreateContext(user.ExternalIdentities.Single()),
            CreateRequest("manage", workspace.Id),
            CancellationToken.None);

        Assert.False(decision.IsAllowed);
    }

    private static User CreateUser()
    {
        return User.Register(
            Guid.NewGuid(),
            new ExternalIdentity(
                "https://issuer.example/identity",
                "external-subject",
                "oidc"),
            new UserProfile("user@example.com", "Vinyl User"),
            DateTimeOffset.UtcNow);
    }

    private static AuthenticatedUserContext CreateContext(ExternalIdentity identity)
    {
        return new AuthenticatedUserContext(
            identity.Issuer,
            identity.Subject,
            identity.Provider,
            "user@example.com",
            "Vinyl User",
            null,
            null,
            null,
            [],
            new Dictionary<string, string>());
    }

    private static AuthorizationRequest CreateRequest(string action, Guid workspaceId)
    {
        var workspaceIdValue = workspaceId.ToString();
        return new AuthorizationRequest(
            action,
            "workspace",
            workspaceIdValue,
            workspaceIdValue);
    }

    private sealed class InMemoryUserRepository(User user) : IUserRepository
    {
        public Task<User?> FindByIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<User?>(user.Id == userId ? user : null);
        }

        public Task<User?> FindByExternalIdentityAsync(
            string issuer,
            string subject,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = user.ExternalIdentities.Any(identity =>
                identity.Issuer == issuer && identity.Subject == subject);
            return Task.FromResult<User?>(matches ? user : null);
        }

        public Task SaveAsync(User userToSave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryWorkspaceRepository(
        Workspace workspace,
        Membership? membership) : IWorkspaceRepository
    {
        public Task<Workspace?> FindByIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Workspace?>(workspace.Id == workspaceId ? workspace : null);
        }

        public Task<Membership?> FindMembershipAsync(
            Guid userId,
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = membership is not null &&
                membership.UserId == userId &&
                membership.WorkspaceId == workspaceId;
            return Task.FromResult(matches ? membership : null);
        }

        public Task<IReadOnlyCollection<Membership>> ListMembershipsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = membership is not null && membership.WorkspaceId == workspaceId
                ? [membership]
                : Array.Empty<Membership>();
            return Task.FromResult<IReadOnlyCollection<Membership>>(result);
        }

        public Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = membership is not null &&
                membership.UserId == userId &&
                membership.IsActive
                ? [new WorkspaceAccess(workspace, membership)]
                : Array.Empty<WorkspaceAccess>();
            return Task.FromResult<IReadOnlyCollection<WorkspaceAccess>>(result);
        }

        public Task CreateAsync(
            Workspace workspaceToCreate,
            Membership membershipToCreate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SaveMembershipAsync(
            Membership membershipToSave,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
