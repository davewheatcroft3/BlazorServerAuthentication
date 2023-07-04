using IdentityModel.Client;
using BlazerServerAuthentication.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BlazerServerAuthentication
{
    public interface IRefreshTokenService
    {
        Task<Tokens> GetTokensCheckIfRefreshNeededAsync(HttpContext context);
    }

    internal class RefreshTokenService : IRefreshTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly OAuthSettings _oAuthSettings;
        private readonly BlazorServerAuthenticationSettings _settings;

        private const string _utcFormat = "yyyy-MM-ddTHH:mm:ss.0000000+00:00";

        public RefreshTokenService(
            HttpClient httpClient,
            IOptions<OAuthSettings> oAuthSettings,
            IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _httpClient = httpClient;
            _oAuthSettings = oAuthSettings.Value;
            _settings = settings.Value;
        }

        internal async Task<TokenResponse> RefreshWithTokenAsync(string refreshToken)
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

        internal bool CheckTokenIsExpired(string expiresAt)
        {
            var expire = DateTime.Parse(expiresAt);
            if (DateTime.Now.AddMinutes(_settings.RefreshExpiryClockSkewInMinutes) >= expire)
            {
                return true;
            }

            return false;
        }

        internal string? ExpiresInToExpiresAt(int expiresInSeconds)
        {
            var newExpiryTime = DateTime.UtcNow.AddSeconds(expiresInSeconds).ToString(_utcFormat, CultureInfo.InvariantCulture);
            return newExpiryTime;
        }

        public async Task<Tokens> GetTokensCheckIfRefreshNeededAsync(HttpContext context)
        {
            var idToken = await context.GetTokenAsync("id_token");
            var accessToken = await context.GetTokenAsync("access_token");
            var refreshToken = await context.GetTokenAsync("refresh_token");
            var expiresAt = await context.GetTokenAsync("expires_at");

            if (!string.IsNullOrEmpty(expiresAt) && !string.IsNullOrEmpty(refreshToken))
            {
                if (CheckTokenIsExpired(expiresAt))
                {
                    var auth = await context.AuthenticateAsync();

                    if (!auth.Succeeded)
                    {
                        await context.SignOutAsync();
                        return new Tokens(null, null, null, null);
                    }

                    if (refreshToken == null)
                    {
                        await context.SignOutAsync();
                        return new Tokens(null, null, null, null);
                    }

                    var tokenResponse = await RefreshWithTokenAsync(refreshToken);

                    if (tokenResponse.IsError)
                    {
                        await context.SignOutAsync();
                        return new Tokens(null, null, null, null);
                    }

                    var newExpiryTime = ExpiresInToExpiresAt(tokenResponse.ExpiresIn);
                    var expiresAtTokenUpdated = false;
                    if (newExpiryTime != null)
                    {
                        expiresAtTokenUpdated = auth.Properties!.UpdateTokenValue("expires_at", newExpiryTime);
                    }

                    var accessTokenUpdated = false;
                    if (tokenResponse.AccessToken != null)
                    {
                        accessTokenUpdated = auth.Properties!.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                    }

                    var idTokenUpdated = false;
                    if (tokenResponse.IdentityToken != null)
                    {
                        idTokenUpdated = auth.Properties!.UpdateTokenValue("id_token", tokenResponse.IdentityToken);
                    }

                    if (tokenResponse.RefreshToken != null)
                    {
                        auth.Properties!.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken);
                    }

                    var tokensUpdatedCorrectly = expiresAtTokenUpdated && accessTokenUpdated && idTokenUpdated;

                    if (tokensUpdatedCorrectly)
                    {
                        await context.SignInAsync(auth.Principal, auth.Properties);
                    }

                    idToken = tokenResponse.IdentityToken;
                    accessToken = tokenResponse.AccessToken;
                    refreshToken = tokenResponse.RefreshToken;
                    expiresAt = newExpiryTime;
                }
            }

            return new Tokens(idToken, accessToken, refreshToken, expiresAt);
        }
    }
}
