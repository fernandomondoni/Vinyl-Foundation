using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Workspaces;

public sealed class WorkspaceService(
    IWorkspaceRepository workspaceRepository,
    TimeProvider timeProvider) : IWorkspaceService
{
    public async Task<WorkspaceAccess> CreateAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var now = timeProvider.GetUtcNow();
        var workspace = Workspace.Create(Guid.NewGuid(), name, now);
        var membership = Membership.Create(
            Guid.NewGuid(),
            userId,
            workspace.Id,
            DefaultRoles.OwnerId,
            now);

        await workspaceRepository.CreateAsync(
            workspace,
            membership,
            cancellationToken);

        return new WorkspaceAccess(workspace, membership);
    }

    public async Task<WorkspaceAccess?> FindForUserAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var membership = await workspaceRepository.FindMembershipAsync(
            userId,
            workspaceId,
            cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return null;
        }

        var workspace = await workspaceRepository.FindByIdAsync(
            workspaceId,
            cancellationToken);
        return workspace is null
            ? null
            : new WorkspaceAccess(workspace, membership);
    }

    public Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        return workspaceRepository.ListForUserAsync(userId, cancellationToken);
    }
}
