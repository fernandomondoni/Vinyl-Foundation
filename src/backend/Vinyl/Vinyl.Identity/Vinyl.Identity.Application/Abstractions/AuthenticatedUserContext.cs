namespace Vinyl.Identity.Application.Abstractions;

public sealed record AuthenticatedUserContext(
    string Issuer,
    string Subject,
    string Provider,
    string? Email,
    string? DisplayName,
    string? TenantId,
    string? WorkspaceId,
    string? ApplicationId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyDictionary<string, string> Claims,
    string? AccessToken = null)
{
    public string GlobalId => $"{Issuer}|{Subject}";
}
