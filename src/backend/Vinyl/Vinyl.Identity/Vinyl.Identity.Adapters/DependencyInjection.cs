using Microsoft.AspNetCore.Authorization;
using Amazon.CognitoIdentityProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Adapters.Authorization;
using Vinyl.Identity.Adapters.Authentication;
using Vinyl.Identity.Adapters.Cognito;
using Vinyl.Identity.Adapters.Persistence;

namespace Vinyl.Identity.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticatedUserContextAccessor, HttpContextUserContextAccessor>();
        services.AddScoped<IWorkspaceContextAccessor, HttpContextWorkspaceContextAccessor>();
        services.AddScoped<IAuthorizationHandler, WorkspacePermissionHandler>();

        services.AddCognitoAuthentication(configuration);
        services.AddIdentityPersistence(configuration);

        services.AddSingleton<IAmazonCognitoIdentityProvider>(
            static _ => new AmazonCognitoIdentityProviderClient());
        services.AddScoped<CognitoUserDirectory>();
        services.AddScoped<IExternalIdentityDirectory, CognitoUserDirectory>();
        services.AddHttpClient<CognitoUserInfoProfileProvider>();
        services.AddScoped<IExternalIdentityProfileProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<CognitoUserInfoProfileProvider>());

        return services;
    }
}
