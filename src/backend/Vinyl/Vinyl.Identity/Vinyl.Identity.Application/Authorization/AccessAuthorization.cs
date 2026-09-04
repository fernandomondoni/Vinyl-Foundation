using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Application.Authorization;

public sealed record AuthorizationRequest(
    string Action,
    string ResourceType,
    string ResourceId,
    string? WorkspaceId);

public sealed record AuthorizationDecision(bool IsAllowed, string? Reason = null)
{
    public static AuthorizationDecision Allow() => new(true);

    public static AuthorizationDecision Deny(string reason) => new(false, reason);
}

public interface IAccessAuthorizationService
{
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthenticatedUserContext principal,
        AuthorizationRequest request,
        CancellationToken cancellationToken);
}
