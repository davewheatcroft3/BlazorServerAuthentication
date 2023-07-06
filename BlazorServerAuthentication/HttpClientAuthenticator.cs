using System.Net.Http.Headers;
using BlazerServerAuthentication;

namespace BlazorServerAuthentication;

public interface IHttpClientAuthenticator
{
    Task PrepareHttpClientAsync(HttpClient httpClient);
}

internal class HttpClientAuthenticator : IHttpClientAuthenticator
{
    private readonly RefreshTokenService _refreshTokenService;

    internal HttpClientAuthenticator(RefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task PrepareHttpClientAsync(HttpClient httpClient)
    {
        if (await _refreshTokenService.CheckIfRefreshNeededAsync())
        {
            await _refreshTokenService.RefreshTokensAsync();
        }

        var token = await _refreshTokenService.GetBearerTokenAsync();

        if (token != null)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
        }
    }
}