using System.Net.Http.Headers;
using BlazerServerAuthentication;

namespace BlazorServerAuthentication;

public interface IHttpClientAuthenticator
{
    Task PrepareHttpClientAsync(HttpClient httpClient);
}

public class HttpClientAuthenticator : IHttpClientAuthenticator
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
            var refreshed = await _refreshTokenService.RefreshTokensAsync();
            if (!refreshed)
            {

            }
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