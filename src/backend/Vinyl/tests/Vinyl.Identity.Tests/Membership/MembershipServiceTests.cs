using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;
using Vinyl.Identity.Application.Memberships;

namespace Vinyl.Identity.Tests.Memberships;

public sealed class MembershipServiceTests
{
    [Fact]
    public async Task AddCreatesAnActiveMemberWithTheRequestedRole()
    {
        var workspace = CreateWorkspace();
        var user = CreateUser();
        var repository = new InMemoryRepository(workspace, user);
        var service = CreateService(repository);

        var result = await service.AddAsync(
            workspace.Id,
            user.Id,
            "Member",
            CancellationToken.None);

        Assert.Equal(MembershipOperationStatus.Succeeded, result.Status);
        Assert.NotNull(result.Member);
        Assert.Equal(DefaultRoles.MemberId, result.Member.Membership.RoleId);
        Assert.True(result.Member.Membership.IsActive);
    }

    [Fact]
    public async Task ChangeRoleUpdatesAnExistingMember()
    {
        var workspace = CreateWorkspace();
        var user = CreateUser();
        var membership = Membership.Create(
            Guid.NewGuid(),
            user.Id,
            workspace.Id,
            DefaultRoles.MemberId,
            DateTimeOffset.UtcNow);
        var repository = new InMemoryRepository(workspace, user, membership);
        var service = CreateService(repository);

        var result = await service.ChangeRoleAsync(
            workspace.Id,
            user.Id,
            "Admin",
            CancellationToken.None);

        Assert.Equal(MembershipOperationStatus.Succeeded, result.Status);
        Assert.NotNull(result.Member);
        Assert.Equal(DefaultRoles.AdminId, result.Member.Membership.RoleId);
    }

    [Fact]
    public async Task LastOwnerCannotBeDeactivated()
    {
        var workspace = CreateWorkspace();
        var user = CreateUser();
        var membership = Membership.Create(
            Guid.NewGuid(),
            user.Id,
            workspace.Id,
            DefaultRoles.OwnerId,
            DateTimeOffset.UtcNow);
        var repository = new InMemoryRepository(workspace, user, membership);
        var service = CreateService(repository);

        var result = await service.DeactivateAsync(
            workspace.Id,
            user.Id,
            CancellationToken.None);

        Assert.Equal(MembershipOperationStatus.LastOwner, result.Status);
        Assert.True(membership.IsActive);
    }

    [Fact]
    public async Task DuplicateMembershipIsRejected()
    {
        var workspace = CreateWorkspace();
        var user = CreateUser();
        var membership = Membership.Create(
            Guid.NewGuid(),
            user.Id,
            workspace.Id,
            DefaultRoles.MemberId,
            DateTimeOffset.UtcNow);
        var repository = new InMemoryRepository(workspace, user, membership);
        var service = CreateService(repository);

        var result = await service.AddAsync(
            workspace.Id,
            user.Id,
            "Viewer",
            CancellationToken.None);

        Assert.Equal(MembershipOperationStatus.AlreadyMember, result.Status);
    }

    private static MembershipService CreateService(InMemoryRepository repository)
    {
        return new MembershipService(
            repository,
            repository,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero)));
    }

    private static Workspace CreateWorkspace() => Workspace.Create(
        Guid.NewGuid(),
        "My Workspace",
        DateTimeOffset.UtcNow);

    private static User CreateUser() => User.Register(
        Guid.NewGuid(),
        new ExternalIdentity("https://issuer.example", "subject", "oidc"),
        new UserProfile("user@example.com", "Vinyl User"),
        DateTimeOffset.UtcNow);

    private sealed class InMemoryRepository(
        Workspace workspace,
        User user,
        Membership? existingMembership = null) : IUserRepository, IWorkspaceRepository
    {
        private readonly List<Membership> memberships = existingMembership is null
            ? []
            : [existingMembership];

        Task<User?> IUserRepository.FindByIdAsync(
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

        Task<Workspace?> IWorkspaceRepository.FindByIdAsync(
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
            return Task.FromResult(memberships.FirstOrDefault(membership =>
                membership.UserId == userId && membership.WorkspaceId == workspaceId));
        }

        public Task<IReadOnlyCollection<Membership>> ListMembershipsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<Membership>>(
                memberships.Where(membership => membership.WorkspaceId == workspaceId).ToArray());
        }

        public Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = memberships
                .Where(membership => membership.UserId == userId && membership.IsActive)
                .Select(membership => new WorkspaceAccess(workspace, membership))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<WorkspaceAccess>>(result);
        }

        public Task CreateAsync(
            Workspace workspaceToCreate,
            Membership membershipToCreate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            memberships.Add(membershipToCreate);
            return Task.CompletedTask;
        }

        public Task SaveMembershipAsync(
            Membership membership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!memberships.Contains(membership))
            {
                memberships.Add(membership);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
