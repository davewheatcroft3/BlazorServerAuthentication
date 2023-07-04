using BlazerServerAuthentication.Configuration;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace BlazerServerAuthentication.Handlers
{
    internal class HttpRefreshTokenTokenHandler : DelegatingHandler
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly OAuthAuthenticationStateProvider _stateProvider;
        private readonly BlazorServerAuthenticationSettings _settings;

        public HttpRefreshTokenTokenHandler(
            ITokenProvider tokenProvider,
            RefreshTokenService refreshTokenService,
            OAuthAuthenticationStateProvider stateProvider,
            IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _tokenProvider = tokenProvider;
            _refreshTokenService = refreshTokenService;
            _stateProvider = stateProvider;
            _settings = settings.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // We dont check expires_at - just assume non null tokens and unauthorized
                // coming back means try refreshing
                var refreshed = await _refreshTokenService.RefreshTokensAsync();

                var idToken = await _tokenProvider.GetIdTokenAsync();
                var accessToken = await _tokenProvider.GetAccessTokenAsync();

                if (refreshed)
                {
                    var token = _settings.UseIdTokenForHttpAuthentication
                                    ? idToken
                                    : accessToken;
                    if (token != null)
                    {
                        // Set the token for this request to be resent, next request wont need this as will pick from token provider
                        // Cookies will only be updated on next full page load however
                        request.SetBearerToken(token);
                        var tryAgainResponse = await base.SendAsync(request, cancellationToken);

                        if (tryAgainResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        {
                            return tryAgainResponse;
                        }
                    }
                }

                await _stateProvider.ClearAuthenticationStateAsync();
            }

            return response;
        }
    }
}
