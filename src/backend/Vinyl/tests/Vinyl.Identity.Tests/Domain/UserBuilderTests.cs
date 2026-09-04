using System.Text.Json;
using Vinyl.Identity.Application.Abstractions;
using Vinyl.Identity.Application.Domain;

namespace Vinyl.Identity.Tests.Domain;

public sealed class UserBuilderTests
{
    [Fact]
    public void BuildCreatesAUserFromTheExternalIdentityContext()
    {
        var context = new AuthenticatedUserContext(
            "https://cognito-idp.sa-east-1.amazonaws.com/sa-east-1_example",
            "external-subject",
            "cognito",
            "user@example.com",
            "Vinyl User",
            null,
            null,
            "client-id",
            ["member"],
            new Dictionary<string, string>
            {
                ["sub"] = "external-subject"
            });
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        var user = new UserBuilder()
            .FromAuthenticatedContext(context, now)
            .Build();

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("user@example.com", user.Profile.Email);
        Assert.Equal("Vinyl User", user.Profile.DisplayName);
        Assert.Equal("external-subject", user.ExternalIdentities.Single().Subject);
        Assert.Equal(now, user.CreatedAt);
    }

    [Fact]
    public void UserCanBeDeserializedFromMartenDocumentJson()
    {
        var context = new AuthenticatedUserContext(
            "https://cognito-idp.sa-east-1.amazonaws.com/sa-east-1_example",
            "external-subject",
            "cognito",
            "user@example.com",
            "Vinyl User",
            null,
            null,
            "client-id",
            [],
            new Dictionary<string, string>());
        var user = new UserBuilder()
            .FromAuthenticatedContext(context, new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero))
            .Build();

        var json = JsonSerializer.Serialize(user);
        var restoredUser = JsonSerializer.Deserialize<User>(json)
            ?? throw new InvalidOperationException("The user could not be deserialized.");

        Assert.Equal(user.Id, restoredUser.Id);
        Assert.Equal(user.ExternalIdentities, restoredUser.ExternalIdentities);
        Assert.Equal(user.Profile, restoredUser.Profile);
        Assert.Equal(user.Status, restoredUser.Status);
        Assert.Equal(user.CreatedAt, restoredUser.CreatedAt);
        Assert.Equal(user.UpdatedAt, restoredUser.UpdatedAt);
    }
}
