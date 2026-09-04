using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;

namespace Vinyl.Identity.Adapters.Authorization;

public sealed class WorkspacePermissionHandler(
    IAccessAuthorizationService accessAuthorizationService,
    IAuthenticatedUserContextAccessor authenticatedUserContextAccessor,
    IWorkspaceContextAccessor workspaceContextAccessor,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<WorkspacePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspacePermissionRequirement requirement)
    {
        var principal = authenticatedUserContextAccessor.Current;
        var workspaceId = workspaceContextAccessor.CurrentWorkspaceId;
        if (principal is null || workspaceId is null)
        {
            return;
        }

        var routeWorkspaceId = GetRouteWorkspaceId();
        if (routeWorkspaceId is not null && routeWorkspaceId != workspaceId)
        {
            return;
        }

        var decision = await accessAuthorizationService.AuthorizeAsync(
            principal,
            new AuthorizationRequest(
                requirement.Action,
                requirement.ResourceType,
                workspaceId.Value.ToString(),
                workspaceId.Value.ToString()),
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }

    private Guid? GetRouteWorkspaceId()
    {
        var routeValue = httpContextAccessor.HttpContext?
            .GetRouteValue("workspaceId")?
            .ToString();
        return Guid.TryParse(routeValue, out var routeWorkspaceId)
            ? routeWorkspaceId
            : null;
    }
}
