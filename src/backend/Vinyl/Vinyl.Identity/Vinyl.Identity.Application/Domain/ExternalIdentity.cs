namespace Vinyl.Identity.Application.Domain;

public sealed record ExternalIdentity(string Issuer, string Subject, string Provider);
