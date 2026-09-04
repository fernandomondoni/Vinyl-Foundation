using JasperFx;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Adapters.Persistence;

public static class MartenPersistenceExtensions
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"] ?? "marten";

        if (string.Equals(provider, "memory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
            return services;
        }

        if (!string.Equals(provider, "marten", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported identity persistence provider '{provider}'.");
        }

        var connectionString = configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Identity must be configured when Persistence:Provider is 'marten'.");
        }

        var autoCreateSchema = configuration.GetValue<bool>("Persistence:AutoCreateSchema");
        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.DatabaseSchemaName = "vinyl_identity";
            options.AutoCreateSchemaObjects = autoCreateSchema
                ? AutoCreate.CreateOrUpdate
                : AutoCreate.None;
            options.Schema.For<User>().Identity(user => user.Id);
            options.Schema.For<Workspace>().Identity(workspace => workspace.Id);
            options.Schema.For<Membership>().Identity(membership => membership.Id);
        });
        services.AddScoped<IUserRepository, MartenUserRepository>();
        services.AddScoped<IWorkspaceRepository, MartenWorkspaceRepository>();
        return services;
    }
}
