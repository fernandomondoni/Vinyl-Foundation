using System.Text.Json.Serialization;

namespace Vinyl.Identity.Application.Domain;

public sealed class Membership
{
    [JsonConstructor]
    private Membership()
    {
    }

    private Membership(
        Guid id,
        Guid userId,
        Guid workspaceId,
        Guid roleId,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        WorkspaceId = workspaceId;
        RoleId = roleId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public Guid UserId { get; private set; }

    [JsonInclude]
    public Guid WorkspaceId { get; private set; }

    [JsonInclude]
    public Guid RoleId { get; private set; }

    [JsonInclude]
    public bool IsActive { get; private set; }

    [JsonInclude]
    public DateTimeOffset CreatedAt { get; private set; }

    public static Membership Create(
        Guid id,
        Guid userId,
        Guid workspaceId,
        Guid roleId,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Membership id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace id cannot be empty.", nameof(workspaceId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        return new Membership(id, userId, workspaceId, roleId, createdAt);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void ChangeRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        RoleId = roleId;
    }
}

#pragma warning disable CA1711
public sealed record Permission(string Resource, string Action);
#pragma warning restore CA1711

public sealed record Role(
    Guid Id,
    string Name,
    IReadOnlyCollection<Permission> Permissions);

public sealed record WorkspaceAccess(
    Workspace Workspace,
    Membership Membership);

public sealed record WorkspaceMember(
    User User,
    Membership Membership);

public enum MembershipOperationStatus
{
    Succeeded,
    UserNotFound,
    UserInactive,
    MembershipNotFound,
    AlreadyMember,
    InvalidRole,
    AlreadyInactive,
    LastOwner
}

public sealed record MembershipOperationResult(
    MembershipOperationStatus Status,
    WorkspaceMember? Member = null);
