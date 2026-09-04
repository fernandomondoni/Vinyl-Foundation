using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.API.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/workspaces", ListWorkspaces)
            .RequireAuthorization()
            .WithName("ListWorkspaces")
            .Produces<IReadOnlyCollection<WorkspaceOutput>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/workspaces/{workspaceId:guid}", GetWorkspace)
            .RequireAuthorization(AccessPolicies.WorkspaceRead)
            .WithName("GetWorkspace")
            .Produces<WorkspaceOutput>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/workspaces", CreateWorkspace)
            .RequireAuthorization()
            .WithName("CreateWorkspace")
            .Produces<WorkspaceOutput>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/workspaces/{workspaceId:guid}/members", ListMembers)
            .RequireAuthorization(AccessPolicies.MembersRead)
            .WithName("ListWorkspaceMembers")
            .Produces<IReadOnlyCollection<WorkspaceMemberOutput>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        endpoints.MapPost("/api/workspaces/{workspaceId:guid}/members", AddMember)
            .RequireAuthorization(AccessPolicies.MembersManage)
            .WithName("AddWorkspaceMember")
            .Produces<WorkspaceMemberOutput>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        endpoints.MapPatch(
                "/api/workspaces/{workspaceId:guid}/members/{userId:guid}",
                ChangeMemberRole)
            .RequireAuthorization(AccessPolicies.MembersManage)
            .WithName("ChangeWorkspaceMemberRole")
            .Produces<WorkspaceMemberOutput>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        endpoints.MapDelete(
                "/api/workspaces/{workspaceId:guid}/members/{userId:guid}",
                DeactivateMember)
            .RequireAuthorization(AccessPolicies.MembersManage)
            .WithName("DeactivateWorkspaceMember")
            .Produces<WorkspaceMemberOutput>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> ListWorkspaces(
        IAuthenticatedUserContextAccessor contextAccessor,
        IUserIdentityService userIdentityService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var user = await userIdentityService.GetOrCreateAsync(
            context,
            cancellationToken);
        var workspaces = await workspaceService.ListForUserAsync(
            user.Id,
            cancellationToken);

        return Results.Ok(workspaces.Select(WorkspaceOutput.From).ToArray());
    }

    private static async Task<IResult> GetWorkspace(
        Guid workspaceId,
        IAuthenticatedUserContextAccessor contextAccessor,
        IUserIdentityService userIdentityService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var user = await userIdentityService.GetOrCreateAsync(
            context,
            cancellationToken);
        var workspace = await workspaceService.FindForUserAsync(
            user.Id,
            workspaceId,
            cancellationToken);

        return workspace is null
            ? Results.NotFound()
            : Results.Ok(WorkspaceOutput.From(workspace));
    }

    private static async Task<IResult> CreateWorkspace(
        CreateWorkspaceRequest? request,
        IAuthenticatedUserContextAccessor contextAccessor,
        IUserIdentityService userIdentityService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Workspace name is required."]
            });
        }

        var context = contextAccessor.Current;
        if (context is null)
        {
            return Results.Unauthorized();
        }

        var user = await userIdentityService.GetOrCreateAsync(
            context,
            cancellationToken);
        var workspace = await workspaceService.CreateAsync(
            user.Id,
            request.Name,
            cancellationToken);
        var output = WorkspaceOutput.From(workspace);

        return Results.Created($"/api/workspaces/{output.Id}", output);
    }

    private static async Task<IResult> ListMembers(
        Guid workspaceId,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        var members = await membershipService.ListAsync(
            workspaceId,
            cancellationToken);
        return Results.Ok(members.Select(WorkspaceMemberOutput.From).ToArray());
    }

    private static async Task<IResult> AddMember(
        Guid workspaceId,
        AddWorkspaceMemberRequest? request,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["userId"] = ["A valid userId is required."]
            });
        }

        var result = await membershipService.AddAsync(
            workspaceId,
            request.UserId,
            request.Role,
            cancellationToken);
        return result.Status switch
        {
            MembershipOperationStatus.Succeeded => Results.Created(
                $"/api/workspaces/{workspaceId}/members/{request.UserId}",
                WorkspaceMemberOutput.From(result.Member!)),
            MembershipOperationStatus.UserNotFound => Results.NotFound(),
            MembershipOperationStatus.UserInactive => Results.Conflict(new
            {
                error = "The target user is not active."
            }),
            MembershipOperationStatus.AlreadyMember => Results.Conflict(new
            {
                error = "The user is already a member of this workspace."
            }),
            MembershipOperationStatus.InvalidRole => InvalidRoleResult(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> ChangeMemberRole(
        Guid workspaceId,
        Guid userId,
        ChangeWorkspaceMemberRoleRequest? request,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Role))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = ["Role is required."]
            });
        }

        var result = await membershipService.ChangeRoleAsync(
            workspaceId,
            userId,
            request.Role,
            cancellationToken);
        return result.Status switch
        {
            MembershipOperationStatus.Succeeded => Results.Ok(
                WorkspaceMemberOutput.From(result.Member!)),
            MembershipOperationStatus.MembershipNotFound => Results.NotFound(),
            MembershipOperationStatus.AlreadyInactive => Results.Conflict(new
            {
                error = "The membership is inactive."
            }),
            MembershipOperationStatus.LastOwner => Results.Conflict(new
            {
                error = "The workspace must have at least one active owner."
            }),
            MembershipOperationStatus.InvalidRole => InvalidRoleResult(),
            MembershipOperationStatus.UserNotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> DeactivateMember(
        Guid workspaceId,
        Guid userId,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        var result = await membershipService.DeactivateAsync(
            workspaceId,
            userId,
            cancellationToken);
        return result.Status switch
        {
            MembershipOperationStatus.Succeeded => Results.Ok(
                WorkspaceMemberOutput.From(result.Member!)),
            MembershipOperationStatus.MembershipNotFound => Results.NotFound(),
            MembershipOperationStatus.AlreadyInactive => Results.Conflict(new
            {
                error = "The membership is already inactive."
            }),
            MembershipOperationStatus.LastOwner => Results.Conflict(new
            {
                error = "The workspace must have at least one active owner."
            }),
            MembershipOperationStatus.UserNotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static IResult InvalidRoleResult()
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["role"] = ["Role must be Owner, Admin, Member or Viewer."]
        });
    }
}
