using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.API.Endpoints;

public sealed record CreateWorkspaceRequest(string Name);

public sealed record WorkspaceOutput(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    string Role)
{
    public static WorkspaceOutput From(WorkspaceAccess access)
    {
        var role = DefaultRoles.Find(access.Membership.RoleId)?.Name ?? "Unknown";
        return new WorkspaceOutput(
            access.Workspace.Id,
            access.Workspace.Name,
            access.Workspace.CreatedAt,
            role);
    }
}

public sealed record AddWorkspaceMemberRequest(
    Guid UserId,
    string Role);

public sealed record ChangeWorkspaceMemberRoleRequest(
    string Role);

public sealed record WorkspaceMemberOutput(
    Guid UserId,
    string? Email,
    string? DisplayName,
    string UserStatus,
    string Role,
    bool IsActive,
    DateTimeOffset JoinedAt)
{
    public static WorkspaceMemberOutput From(WorkspaceMember member)
    {
        var role = DefaultRoles.Find(member.Membership.RoleId)?.Name ?? "Unknown";
        return new WorkspaceMemberOutput(
            member.User.Id,
            member.User.Profile.Email,
            member.User.Profile.DisplayName,
            member.User.Status.ToString(),
            role,
            member.Membership.IsActive,
            member.Membership.CreatedAt);
    }
}
