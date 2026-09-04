using Microsoft.AspNetCore.Authorization;

namespace Vinyl.Identity.Adapters.Authorization;

public sealed record WorkspacePermissionRequirement(
    string ResourceType,
    string Action) : IAuthorizationRequirement;
