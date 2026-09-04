using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Adapters.Authentication;

public sealed class HttpContextUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IAuthenticatedUserContextAccessor
{
    public AuthenticatedUserContext? Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            var principal = httpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var issuer = GetClaim(principal, "iss");
            var subject = GetClaim(principal, "sub");
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
            {
                return null;
            }

            var claims = principal.Claims
                .GroupBy(claim => claim.Type, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Value,
                    StringComparer.Ordinal);

            var roles = principal.Claims
                .Where(claim => claim.Type is "cognito:groups" or "roles" or ClaimTypes.Role)
                .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return new AuthenticatedUserContext(
                issuer,
                subject,
                "cognito",
                GetClaim(principal, "email"),
                GetClaim(principal, "name") ?? GetClaim(principal, "preferred_username"),
                GetClaim(principal, "tenant_id") ?? GetClaim(principal, "custom:tenant_id"),
                GetClaim(principal, "workspace_id") ?? GetClaim(principal, "custom:workspace_id"),
                GetClaim(principal, "client_id"),
                roles,
                claims,
                GetBearerToken(httpContext));
        }
    }

    private static string? GetClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }

    private static string? GetBearerToken(HttpContext? httpContext)
    {
        var authorization = httpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization)
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorization["Bearer ".Length..].Trim();
    }
}
