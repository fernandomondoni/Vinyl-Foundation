using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IWorkspaceRepository
{
    Task<Workspace?> FindByIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<Membership?> FindMembershipAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Membership>> ListMembershipsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task CreateAsync(
        Workspace workspace,
        Membership membership,
        CancellationToken cancellationToken);

    Task SaveMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken);
}
