using System.Collections.Concurrent;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly ConcurrentDictionary<Guid, Workspace> workspaces = new();
    private readonly ConcurrentDictionary<Guid, Membership> memberships = new();

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
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(membership);
        cancellationToken.ThrowIfCancellationRequested();

        if (!workspaces.TryAdd(workspace.Id, workspace))
        {
            throw new InvalidOperationException(
                $"Workspace '{workspace.Id}' already exists.");
        }

        if (!memberships.TryAdd(membership.Id, membership))
        {
            workspaces.TryRemove(workspace.Id, out _);
            throw new InvalidOperationException(
                $"Membership '{membership.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task SaveMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(membership);
        cancellationToken.ThrowIfCancellationRequested();
        memberships[membership.Id] = membership;
        return Task.CompletedTask;
    }
}
