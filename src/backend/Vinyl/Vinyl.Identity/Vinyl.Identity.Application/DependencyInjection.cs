using Microsoft.Extensions.DependencyInjection;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Authorization;
using Vinyl.Identity.Application.Identity;
using Vinyl.Identity.Application.Memberships;
using Vinyl.Identity.Application.Subscriptions;
using Vinyl.Identity.Application.Workspaces;

namespace Vinyl.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAccessAuthorizationService, AccessAuthorizationService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IContentAccessService, ContentAccessService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        return services;
    }
}
