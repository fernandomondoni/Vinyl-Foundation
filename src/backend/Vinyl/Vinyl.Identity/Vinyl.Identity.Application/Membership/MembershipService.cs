using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Memberships;

public sealed class MembershipService(
    IUserRepository userRepository,
    IWorkspaceRepository workspaceRepository,
    TimeProvider timeProvider) : IMembershipService
{
    public async Task<IReadOnlyCollection<WorkspaceMember>> ListAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var memberships = await workspaceRepository.ListMembershipsAsync(
            workspaceId,
            cancellationToken);
        var members = new List<WorkspaceMember>(memberships.Count);

        foreach (var membership in memberships)
        {
            var user = await userRepository.FindByIdAsync(
                membership.UserId,
                cancellationToken);
            if (user is not null)
            {
                members.Add(new WorkspaceMember(user, membership));
            }
        }

        return members;
    }

    public async Task<MembershipOperationResult> AddAsync(
        Guid workspaceId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.UserNotFound);
        }

        if (user.Status != UserStatus.Active)
        {
            return new MembershipOperationResult(MembershipOperationStatus.UserInactive);
        }

        var role = DefaultRoles.FindByName(roleName);
        if (role is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.InvalidRole);
        }

        var existingMembership = await workspaceRepository.FindMembershipAsync(
            userId,
            workspaceId,
            cancellationToken);
        if (existingMembership is not null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.AlreadyMember);
        }

        var membership = Membership.Create(
            Guid.NewGuid(),
            userId,
            workspaceId,
            role.Id,
            timeProvider.GetUtcNow());
        await workspaceRepository.SaveMembershipAsync(
            membership,
            cancellationToken);

        return new MembershipOperationResult(
            MembershipOperationStatus.Succeeded,
            new WorkspaceMember(user, membership));
    }

    public async Task<MembershipOperationResult> ChangeRoleAsync(
        Guid workspaceId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var role = DefaultRoles.FindByName(roleName);
        if (role is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.InvalidRole);
        }

        var membership = await workspaceRepository.FindMembershipAsync(
            userId,
            workspaceId,
            cancellationToken);
        if (membership is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.MembershipNotFound);
        }

        if (!membership.IsActive)
        {
            return new MembershipOperationResult(MembershipOperationStatus.AlreadyInactive);
        }

        if (membership.RoleId == DefaultRoles.OwnerId && role.Id != DefaultRoles.OwnerId)
        {
            var memberships = await workspaceRepository.ListMembershipsAsync(
                workspaceId,
                cancellationToken);
            var activeOwnerCount = memberships.Count(candidate =>
                candidate.IsActive && candidate.RoleId == DefaultRoles.OwnerId);
            if (activeOwnerCount == 1)
            {
                return new MembershipOperationResult(MembershipOperationStatus.LastOwner);
            }
        }

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.UserNotFound);
        }

        membership.ChangeRole(role.Id);
        await workspaceRepository.SaveMembershipAsync(
            membership,
            cancellationToken);

        return new MembershipOperationResult(
            MembershipOperationStatus.Succeeded,
            new WorkspaceMember(user, membership));
    }

    public async Task<MembershipOperationResult> DeactivateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await workspaceRepository.FindMembershipAsync(
            userId,
            workspaceId,
            cancellationToken);
        if (membership is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.MembershipNotFound);
        }

        if (!membership.IsActive)
        {
            return new MembershipOperationResult(MembershipOperationStatus.AlreadyInactive);
        }

        if (membership.RoleId == DefaultRoles.OwnerId)
        {
            var memberships = await workspaceRepository.ListMembershipsAsync(
                workspaceId,
                cancellationToken);
            var activeOwnerCount = memberships.Count(candidate =>
                candidate.IsActive && candidate.RoleId == DefaultRoles.OwnerId);
            if (activeOwnerCount == 1)
            {
                return new MembershipOperationResult(MembershipOperationStatus.LastOwner);
            }
        }

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new MembershipOperationResult(MembershipOperationStatus.UserNotFound);
        }

        membership.Deactivate();
        await workspaceRepository.SaveMembershipAsync(
            membership,
            cancellationToken);

        return new MembershipOperationResult(
            MembershipOperationStatus.Succeeded,
            new WorkspaceMember(user, membership));
    }
}
