using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Application.Authorization;

public static class DefaultRoles
{
    public static readonly Guid OwnerId = Guid.Parse("7e5d6d71-3b5e-4c5f-8e64-8b3f9be6d6c1");
    public static readonly Guid AdminId = Guid.Parse("f9f4b5b2-fd3e-45c0-bd55-12c9a3d6f601");
    public static readonly Guid MemberId = Guid.Parse("c44c0d9e-9d3f-4e8b-aef4-1d13adf6738b");
    public static readonly Guid ViewerId = Guid.Parse("bbf4c10e-1f66-4ee6-a1cc-7bb2d9c5e92f");

    public static readonly Role Owner = new(
        OwnerId,
        "Owner",
        [
            new Permission("workspace", "read"),
            new Permission("workspace", "manage"),
            new Permission("members", "read"),
            new Permission("members", "manage"),
            new Permission("roles", "manage")
        ]);

    public static readonly Role Admin = new(
        AdminId,
        "Admin",
        [
            new Permission("workspace", "read"),
            new Permission("members", "read"),
            new Permission("members", "manage")
        ]);

    public static readonly Role Member = new(
        MemberId,
        "Member",
        [new Permission("workspace", "read")]);

    public static readonly Role Viewer = new(
        ViewerId,
        "Viewer",
        [new Permission("workspace", "read")]);

    public static Role? Find(Guid roleId) => roleId switch
    {
        var id when id == Owner.Id => Owner,
        var id when id == Admin.Id => Admin,
        var id when id == Member.Id => Member,
        var id when id == Viewer.Id => Viewer,
        _ => null
    };

    public static Role? FindByName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        return new[] { Owner, Admin, Member, Viewer }
            .FirstOrDefault(role =>
                string.Equals(role.Name, roleName.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
