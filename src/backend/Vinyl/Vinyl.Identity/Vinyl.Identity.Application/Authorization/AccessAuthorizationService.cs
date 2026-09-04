using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Authorization;

public sealed class AccessAuthorizationService(
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository) : IAccessAuthorizationService
{
    public async Task<AuthorizationDecision> AuthorizeAsync(
        AuthenticatedUserContext principal,
        AuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.FindByExternalIdentityAsync(
            principal.Issuer,
            principal.Subject,
            cancellationToken);
        if (user is null)
        {
            return AuthorizationDecision.Deny("The local user does not exist.");
        }

        if (user.Status != UserStatus.Active)
        {
            return AuthorizationDecision.Deny("The local user is not active.");
        }

        if (!Guid.TryParse(request.WorkspaceId, out var workspaceId) ||
            workspaceId == Guid.Empty)
        {
            return AuthorizationDecision.Deny("A valid workspace is required.");
        }

        if (!Guid.TryParse(request.ResourceId, out var resourceId) ||
            resourceId != workspaceId)
        {
            return AuthorizationDecision.Deny("The resource does not match the workspace.");
        }

        var membership = await workspaceRepository.FindMembershipAsync(
            user.Id,
            workspaceId,
            cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return AuthorizationDecision.Deny("The user is not an active workspace member.");
        }

        var role = DefaultRoles.Find(membership.RoleId);
        if (role is null)
        {
            return AuthorizationDecision.Deny("The membership role does not exist.");
        }

        var isAllowed = role.Permissions.Any(permission =>
            string.Equals(permission.Resource, request.ResourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(permission.Action, request.Action, StringComparison.OrdinalIgnoreCase));

        return isAllowed
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny("The membership role does not grant this permission.");
    }
}
