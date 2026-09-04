using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.API.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/identity/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<CurrentUserOutput>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/identity/me/access", GetCurrentUserContentAccess)
            .RequireAuthorization()
            .WithName("GetCurrentUserContentAccess")
            .Produces<ContentAccessOutput>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentUser(
        IAuthenticatedUserContextAccessor contextAccessor,
        IUserIdentityService userIdentityService,
        CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var user = await userIdentityService.GetOrCreateAsync(context, cancellationToken);
        return Results.Ok(new CurrentUserOutput(
            user.Id,
            context.GlobalId,
            user.Profile.Email,
            user.Profile.DisplayName,
            user.Status.ToString(),
            context.Roles));
    }

    private static async Task<IResult> GetCurrentUserContentAccess(
        IAuthenticatedUserContextAccessor contextAccessor,
        IUserIdentityService userIdentityService,
        IContentAccessService contentAccessService,
        CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var user = await userIdentityService.GetOrCreateAsync(context, cancellationToken);
        var access = await contentAccessService.GetForUserAsync(
            user.Id,
            cancellationToken);

        return Results.Ok(ContentAccessOutput.From(access));
    }
}
