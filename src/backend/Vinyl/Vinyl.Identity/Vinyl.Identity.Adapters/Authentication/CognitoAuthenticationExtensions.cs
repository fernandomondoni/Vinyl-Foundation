using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Adapters.Authorization;

namespace Vinyl.Identity.Adapters.Authentication;

public static class CognitoAuthenticationExtensions
{
    public static IServiceCollection AddCognitoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(CognitoAuthenticationOptions.SectionName)
            .Get<CognitoAuthenticationOptions>() ?? new CognitoAuthenticationOptions();

        services.Configure<CognitoAuthenticationOptions>(
            configuration.GetSection(CognitoAuthenticationOptions.SectionName));

        var authenticationBuilder = services
            .AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            authenticationBuilder.AddScheme<AuthenticationSchemeOptions, MissingAuthenticationHandler>(
                JwtBearerDefaults.AuthenticationScheme,
                static _ => { });
        }
        else
        {
            authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
            {
                jwtOptions.Authority = options.Authority;
                jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwtOptions.MapInboundClaims = false;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Authority,
                    ValidateAudience = options.ValidateAudience,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "cognito:groups"
                };
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenUse = context.Principal?.FindFirst("token_use")?.Value;
                        if (!string.Equals(tokenUse, "access", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Fail("Only Cognito access tokens can call this API.");
                            return Task.CompletedTask;
                        }

                        if (options.AllowedClientIds.Length == 0)
                        {
                            return Task.CompletedTask;
                        }

                        var clientId = context.Principal?.FindFirst("client_id")?.Value;
                        if (clientId is null || !options.AllowedClientIds.Contains(clientId, StringComparer.Ordinal))
                        {
                            context.Fail("The token was not issued to an allowed Cognito app client.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }

        services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy(
                AccessPolicies.WorkspaceRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new WorkspacePermissionRequirement("workspace", "read")));
            authorizationOptions.AddPolicy(
                AccessPolicies.WorkspaceManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new WorkspacePermissionRequirement("workspace", "manage")));
            authorizationOptions.AddPolicy(
                AccessPolicies.MembersRead,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new WorkspacePermissionRequirement("members", "read")));
            authorizationOptions.AddPolicy(
                AccessPolicies.MembersManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new WorkspacePermissionRequirement("members", "manage")));
            authorizationOptions.AddPolicy(
                AccessPolicies.RolesManage,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new WorkspacePermissionRequirement("roles", "manage")));
        });
        return services;
    }
}
