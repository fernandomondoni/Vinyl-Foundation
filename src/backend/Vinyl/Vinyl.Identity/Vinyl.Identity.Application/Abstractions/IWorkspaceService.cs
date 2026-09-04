using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Abstractions;

public interface IWorkspaceService
{
    Task<WorkspaceAccess> CreateAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken);

    Task<WorkspaceAccess?> FindForUserAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkspaceAccess>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
