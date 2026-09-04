using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using Vinyl.Identity.Adapters.Authentication;
using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Adapters.Cognito;

public sealed class CognitoUserDirectory(
    IAmazonCognitoIdentityProvider client,
    IOptions<CognitoAuthenticationOptions> options) : IExternalIdentityDirectory
{
    public async Task<ExternalUserReference> CreateUserAsync(
        ExternalUserRegistration registration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var userPoolId = GetRequiredUserPoolId();

        var attributes = new List<AttributeType>
        {
            new() { Name = "email", Value = registration.Email },
            new() { Name = "email_verified", Value = "true" }
        };

        if (!string.IsNullOrWhiteSpace(registration.DisplayName))
        {
            attributes.Add(new AttributeType { Name = "name", Value = registration.DisplayName });
        }

        var response = await client.AdminCreateUserAsync(
            new AdminCreateUserRequest
            {
                UserPoolId = userPoolId,
                Username = registration.Email,
                UserAttributes = attributes,
                MessageAction = "SUPPRESS"
            },
            cancellationToken);

        var createdUser = response.User
            ?? throw new InvalidOperationException("Cognito did not return the created user.");
        var subject = createdUser.Attributes
            .FirstOrDefault(attribute => attribute.Name == "sub")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("Cognito did not return the subject of the created user.");
        }

        return new ExternalUserReference(
            options.Value.Authority,        
            subject,
            "cognito",
            createdUser.Username);
    }

    public async Task DisableUserAsync(
        ExternalUserReference user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var userPoolId = GetRequiredUserPoolId();

        await client.AdminDisableUserAsync(
            new AdminDisableUserRequest
            {
                UserPoolId = userPoolId,
                Username = user.Username ?? user.Subject
            },
            cancellationToken);
    }

    private string GetRequiredUserPoolId()
    {
        var authority = options.Value.Authority;
        var userPoolId = string.IsNullOrWhiteSpace(options.Value.UserPoolId)
            ? authority.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
            : options.Value.UserPoolId;
        if (string.IsNullOrWhiteSpace(userPoolId))
        {
            throw new InvalidOperationException(
                "The Cognito authority must end with the configured user pool id.");
        }

        return userPoolId;
    }
}
