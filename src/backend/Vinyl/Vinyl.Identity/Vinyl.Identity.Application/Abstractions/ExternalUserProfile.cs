namespace Vinyl.Identity.Application.Abstractions;

public sealed record ExternalUserProfile(
    string? Email,
    string? DisplayName);
