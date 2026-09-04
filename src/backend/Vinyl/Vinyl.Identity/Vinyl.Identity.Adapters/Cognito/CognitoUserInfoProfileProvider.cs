using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vinyl.Identity.Adapters.Authentication;
using Vinyl.Identity.Application.Abstractions;

namespace Vinyl.Identity.Adapters.Cognito;

public sealed class CognitoUserInfoProfileProvider(
    HttpClient httpClient,
    IOptions<CognitoAuthenticationOptions> options,
    ILogger<CognitoUserInfoProfileProvider> logger) : IExternalIdentityProfileProvider
{
    private static readonly Action<ILogger, Exception?> UserInfoEndpointNotConfigured =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(UserInfoEndpointNotConfigured)),
            "Cognito userInfo synchronization was skipped because UserInfoEndpoint is not configured.");

    private static readonly Action<ILogger, int, string, Exception?> UserInfoRequestFailed =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(2, nameof(UserInfoRequestFailed)),
            "Cognito userInfo returned HTTP status {StatusCode} for subject {Subject}.");

    private static readonly Action<ILogger, string, Exception?> UserInfoResponseEmpty =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3, nameof(UserInfoResponseEmpty)),
            "Cognito userInfo returned an empty response for subject {Subject}.");

    public async Task<ExternalUserProfile?> GetAsync(
        AuthenticatedUserContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(context.Provider, "cognito", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(context.AccessToken))
        {
            return null;
        }

        var endpoint = options.Value.UserInfoEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            UserInfoEndpointNotConfigured(logger, null);
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            UserInfoRequestFailed(logger, (int)response.StatusCode, context.Subject, null);
            return null;
        }

        var userInfo = await response.Content.ReadFromJsonAsync<CognitoUserInfoResponse>(
            cancellationToken);
        if (userInfo is null)
        {
            UserInfoResponseEmpty(logger, context.Subject, null);
            return null;
        }

        return new ExternalUserProfile(
            userInfo.Email,
            userInfo.Name ?? userInfo.PreferredUsername ?? userInfo.Username);
    }

    private sealed record CognitoUserInfoResponse(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("preferred_username")] string? PreferredUsername,
        [property: JsonPropertyName("username")] string? Username);
}
