namespace Vinyl.Identity.Application.Abstractions;

public interface IExternalIdentityDirectory
{
    Task<ExternalUserReference> CreateUserAsync(
        ExternalUserRegistration registration,
        CancellationToken cancellationToken);

    Task DisableUserAsync(
        ExternalUserReference user,
        CancellationToken cancellationToken);
}

public sealed record ExternalUserRegistration(
    string Email,
    string? DisplayName);

public sealed record ExternalUserReference(
    string Issuer,
    string Subject,
    string Provider,
    string? Username = null);
