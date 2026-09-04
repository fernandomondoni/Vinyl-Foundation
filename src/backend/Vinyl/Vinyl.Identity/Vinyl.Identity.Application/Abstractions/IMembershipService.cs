using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IMembershipService
{
    Task<IReadOnlyCollection<WorkspaceMember>> ListAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<MembershipOperationResult> AddAsync(
        Guid workspaceId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken);

    Task<MembershipOperationResult> ChangeRoleAsync(
        Guid workspaceId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken);

    Task<MembershipOperationResult> DeactivateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken);
}
