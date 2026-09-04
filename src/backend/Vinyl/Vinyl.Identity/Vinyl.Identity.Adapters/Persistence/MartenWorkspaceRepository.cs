using Marten;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class MartenWorkspaceRepository(IDocumentSession session) : IWorkspaceRepository
{
    public Task<Workspace?> FindByIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return session.LoadAsync<Workspace>(workspaceId, cancellationToken);
    }

    public Task<Membership?> FindMembershipAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return session.Query<Membership>()
            .Where(membership =>
                membership.UserId == userId &&
                membership.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Membership>> ListMembershipsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return await session.Query<Membership>()
            .Where(membership => membership.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var memberships = await session.Query<Membership>()
            .Where(membership =>
                membership.UserId == userId &&
                membership.IsActive)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return [];
        }

        var workspaceIds = memberships
            .Select(membership => membership.WorkspaceId)
            .ToArray();
        var workspaces = await session.Query<Workspace>()
            .Where(workspace => workspaceIds.Contains(workspace.Id))
            .ToListAsync(cancellationToken);
        var workspacesById = workspaces.ToDictionary(workspace => workspace.Id);

        return memberships
            .Where(membership => workspacesById.ContainsKey(membership.WorkspaceId))
            .Select(membership => new WorkspaceAccess(
                workspacesById[membership.WorkspaceId],
                membership))
            .ToArray();
    }

    public async Task CreateAsync(
        Workspace workspace,
        Membership membership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(membership);

        session.Store(workspace);
        session.Store(membership);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(membership);
        session.Store(membership);
        await session.SaveChangesAsync(cancellationToken);
    }
}
