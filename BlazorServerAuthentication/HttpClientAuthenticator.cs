using System.Net.Http.Headers;
using BlazerServerAuthentication;

namespace BlazorServerAuthentication;

/// <summary>
/// Inject this into your HttpClients/ApiClients to add bearer token (if authenticated).
/// </summary>
public interface IHttpClientAuthenticator
{
    /// <summary>
    /// Will check authenticated and if so add the token to the authorization header as a bearer token.
    /// If token expired, it will attempt to refresh the token before (and store with the provider) 
    /// and use the refreshed token as the bearer token.
    /// </summary>
    /// <param name="httpClient">The http client used for the http call.</param>
    /// <returns>True if the user is authorized and whether the refresh token was used successfully.</returns>
    Task<(bool IsAuthenticated, bool WasRefreshed)> PrepareHttpClientAsync(HttpClient httpClient);
}

public class HttpClientAuthenticator : IHttpClientAuthenticator
{
    private readonly RefreshTokenService _refreshTokenService;

    internal HttpClientAuthenticator(RefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<(bool IsAuthenticated, bool WasRefreshed)> PrepareHttpClientAsync(HttpClient httpClient)
    {
        var wasRefreshed = false;
        if (await _refreshTokenService.CheckIfRefreshNeededAsync())
        {
            wasRefreshed = await _refreshTokenService.RefreshTokensAsync();
        }

        var token = await _refreshTokenService.GetBearerTokenAsync();

        if (token != null)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return (true, wasRefreshed);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");

            return (false, false);
        }
    }
}