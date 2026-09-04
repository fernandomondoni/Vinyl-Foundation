namespace Vinyl.Identity.API.Endpoints;

public sealed record CurrentUserOutput(
    Guid UserId,
    string GlobalId,
    string? Email,
    string? DisplayName,
    string Status,
    IReadOnlyCollection<string> Roles);
