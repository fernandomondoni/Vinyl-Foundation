using Microsoft.AspNetCore.Http;
using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Adapters.Authentication;

public sealed class HttpContextWorkspaceContextAccessor(
    IHttpContextAccessor httpContextAccessor) : IWorkspaceContextAccessor
{
    public Guid? CurrentWorkspaceId
    {
        get
        {
            var headerValue = httpContextAccessor.HttpContext?
                .Request.Headers["X-Workspace-Id"]
                .FirstOrDefault();

            return Guid.TryParse(headerValue, out var workspaceId) && workspaceId != Guid.Empty
                ? workspaceId
                : null;
        }
    }
}
