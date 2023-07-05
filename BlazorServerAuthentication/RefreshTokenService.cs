using BlazerServerAuthentication.Configuration;
using IdentityModel.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BlazerServerAuthentication
{
    internal class RefreshTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthSettings _oAuthSettings;
        private readonly ITokenProvider _tokenProvider;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly JwtService _jwtService;
        private readonly BlazorServerAuthenticationSettings _settings;

        public RefreshTokenService(
            HttpClient httpClient,
            ITokenProvider tokenProvider,
            AuthenticationStateProvider authenticationStateProvider,
            JwtService jwtService,
            IOptions<OAuthSettings> oAuthSettings,
            IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
            _authenticationStateProvider = authenticationStateProvider;
            _jwtService = jwtService;
            _oAuthSettings = oAuthSettings.Value;
            _settings = settings.Value;
        }

        public async Task<string?> GetBearerTokenAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var tokens = await _tokenProvider.GetTokensAsync(state.User);

            var token = _settings.UseIdTokenForHttpAuthentication
                                   ? tokens?.IdToken
                                   : tokens?.AccessToken;

            return token;
        }

        public async Task<bool> RefreshTokensAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var refreshed = await RefreshTokensAsync(state.User);

            if (!refreshed && _authenticationStateProvider is ServerAuthenticationStateProvider serverAuthenticationStateProvider)
            {
                var anonymous = new AuthenticationState(new ClaimsPrincipal());
                serverAuthenticationStateProvider.SetAuthenticationState(Task.FromResult(anonymous));
            }

            return refreshed;
        }

        public async Task<bool> RefreshTokensAsync(ClaimsPrincipal user)
        {
            var tokens = await _tokenProvider.GetTokensAsync(user);
            if (tokens?.IdToken != null && tokens?.AccessToken != null && tokens?.RefreshToken != null)
            {
                var tokenResponse = await RefreshWithTokenAsync(tokens.AccessToken);

                if (!tokenResponse.IsError)
                {
                    await _tokenProvider.SetTokensAsync(
                        user,
                        new Tokens(
                            tokenResponse.IdentityToken,
                            tokenResponse.AccessToken,
                            tokenResponse.RefreshToken ?? tokens.RefreshToken));

                    return true;
                }
            }

            return false;
        }

        public async Task RevokeRefreshTokenAsync(ClaimsPrincipal user)
        {
            var tokens = await _tokenProvider.GetTokensAsync(user);

            if (tokens?.RefreshToken != null)
            {
                await _httpClient.RevokeTokenAsync(new TokenRevocationRequest
                {
                    Address = _oAuthSettings.TokenUrl,
                    ClientId = _oAuthSettings.ClientId,
                    ClientSecret = _oAuthSettings.ClientSecret,
                    Token = tokens.RefreshToken
                });

                await _tokenProvider.ClearTokensAsync(user);
            }
        }

        public async Task<bool> CheckIfRefreshNeededAsync(ClaimsPrincipal user)
        {
            var tokens = await _tokenProvider.GetTokensAsync(user);

            var expiry = GetExpiryTime(tokens);
            if (expiry != null)
            {
                if (ShouldRefreshToken(expiry.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> CheckIfRefreshNeededAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return await CheckIfRefreshNeededAsync(state.User);
        }

        private async Task<TokenResponse> RefreshWithTokenAsync(string refreshToken)
        {
            var tokenResponse = await _httpClient.RequestRefreshTokenAsync(
                new RefreshTokenRequest
                {
                    Address = _oAuthSettings.TokenUrl,
                    ClientId = _oAuthSettings.ClientId,
                    ClientSecret = _oAuthSettings.ClientSecret,
                    RefreshToken = refreshToken
                });
            return tokenResponse;
        }

        private bool ShouldRefreshToken(DateTime expiresAt)
        {
            if (DateTime.UtcNow.AddMinutes(_settings.RefreshExpiryClockSkewInMinutes) >= expiresAt)
            {
                return true;
            }

            return false;
        }

        private DateTime? GetExpiryTime(Tokens? tokens)
        {
            if (tokens?.IdToken == null)
            {
                return null;
            }

            var decodedToken = _jwtService.DecodeToken(tokens.IdToken);
            if (decodedToken == null)
            {
                return null;
            }

            return decodedToken.ValidTo;
        }
    }
}
