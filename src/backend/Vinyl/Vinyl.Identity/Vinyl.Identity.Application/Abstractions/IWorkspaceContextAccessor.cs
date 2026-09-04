namespace Vinyl.Identity.Application.Abstractions;

public interface IWorkspaceContextAccessor
{
    Guid? CurrentWorkspaceId { get; }
}
