namespace Vinyl.Identity.Adapters.Authentication;

public sealed class CognitoAuthenticationOptions
{
    public const string SectionName = "Authentication:Cognito";

    public string Authority { get; set; } = string.Empty;

    public string UserPoolId { get; set; } = string.Empty;

    public string UserInfoEndpoint { get; set; } = string.Empty;

    public string? Audience { get; set; }

    public bool ValidateAudience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    public string[] AllowedClientIds { get; set; } = [];
}
