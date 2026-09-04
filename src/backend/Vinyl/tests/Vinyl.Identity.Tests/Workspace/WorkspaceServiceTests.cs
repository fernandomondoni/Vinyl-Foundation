using System.Text.Json;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;
using Vinyl.Identity.Application.Workspaces;

namespace Vinyl.Identity.Tests.Workspaces;

public sealed class WorkspaceServiceTests
{
    [Fact]
    public async Task CreateAssignsTheAuthenticatedUserAsOwner()
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new WorkspaceService(
            repository,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero)));
        var userId = Guid.NewGuid();

        var access = await service.CreateAsync(
            userId,
            "My Workspace",
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, access.Workspace.Id);
        Assert.Equal("My Workspace", access.Workspace.Name);
        Assert.Equal(userId, access.Membership.UserId);
        Assert.Equal(access.Workspace.Id, access.Membership.WorkspaceId);
        Assert.Equal(DefaultRoles.OwnerId, access.Membership.RoleId);
        Assert.True(access.Membership.IsActive);
    }

    [Fact]
    public async Task ListAndFindReturnOnlyWorkspacesForTheUser()
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new WorkspaceService(repository, TimeProvider.System);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var userWorkspace = await service.CreateAsync(
            userId,
            "User Workspace",
            CancellationToken.None);
        var otherWorkspace = await service.CreateAsync(
            otherUserId,
            "Other Workspace",
            CancellationToken.None);

        var workspaces = await service.ListForUserAsync(userId, CancellationToken.None);
        var ownWorkspace = await service.FindForUserAsync(
            userId,
            userWorkspace.Workspace.Id,
            CancellationToken.None);
        var unauthorizedWorkspace = await service.FindForUserAsync(
            userId,
            otherWorkspace.Workspace.Id,
            CancellationToken.None);

        var listedWorkspace = Assert.Single(workspaces);
        Assert.Equal(userWorkspace.Workspace.Id, listedWorkspace.Workspace.Id);
        Assert.NotNull(ownWorkspace);
        Assert.Null(unauthorizedWorkspace);
    }

    [Fact]
    public void WorkspaceAndMembershipCanBeDeserializedFromMartenDocumentJson()
    {
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var workspace = Workspace.Create(Guid.NewGuid(), "My Workspace", now);
        var membership = Membership.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            workspace.Id,
            DefaultRoles.OwnerId,
            now);

        var restoredWorkspace = JsonSerializer.Deserialize<Workspace>(
            JsonSerializer.Serialize(workspace));
        var restoredMembership = JsonSerializer.Deserialize<Membership>(
            JsonSerializer.Serialize(membership));

        Assert.NotNull(restoredWorkspace);
        Assert.NotNull(restoredMembership);
        Assert.Equal(workspace.Id, restoredWorkspace.Id);
        Assert.Equal(workspace.Name, restoredWorkspace.Name);
        Assert.Equal(workspace.CreatedAt, restoredWorkspace.CreatedAt);
        Assert.Equal(membership.Id, restoredMembership.Id);
        Assert.Equal(membership.UserId, restoredMembership.UserId);
        Assert.Equal(membership.WorkspaceId, restoredMembership.WorkspaceId);
        Assert.Equal(membership.RoleId, restoredMembership.RoleId);
        Assert.Equal(membership.IsActive, restoredMembership.IsActive);
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        private readonly Dictionary<Guid, Workspace> workspaces = new();
        private readonly Dictionary<Guid, Membership> memberships = new();

        public Task<Workspace?> FindByIdAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            workspaces.TryGetValue(workspaceId, out var workspace);
            return Task.FromResult(workspace);
        }

        public Task<Membership?> FindMembershipAsync(
            Guid userId,
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var membership = memberships.Values.FirstOrDefault(candidate =>
                candidate.UserId == userId && candidate.WorkspaceId == workspaceId);
            return Task.FromResult(membership);
        }

        public Task<IReadOnlyCollection<Membership>> ListMembershipsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = memberships.Values
                .Where(membership => membership.WorkspaceId == workspaceId)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<Membership>>(result);
        }

        public Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = memberships.Values
                .Where(membership => membership.UserId == userId && membership.IsActive)
                .Join(
                    workspaces.Values,
                    membership => membership.WorkspaceId,
                    workspace => workspace.Id,
                    (membership, workspace) => new WorkspaceAccess(workspace, membership))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<WorkspaceAccess>>(result);
        }

        public Task CreateAsync(
            Workspace workspace,
            Membership membership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            workspaces.Add(workspace.Id, workspace);
            memberships.Add(membership.Id, membership);
            return Task.CompletedTask;
        }

        public Task SaveMembershipAsync(
            Membership membership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            memberships[membership.Id] = membership;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
