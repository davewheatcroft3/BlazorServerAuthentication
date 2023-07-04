using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace BlazerServerAuthentication
{
    public class OAuthAuthenticationStateProvider : ServerAuthenticationStateProvider
    {
        private readonly ITokenProvider _tokenProvider;

        private readonly RefreshTokenService _refreshTokenService;

        internal OAuthAuthenticationStateProvider(ITokenProvider tokenProvider, RefreshTokenService refreshTokenService)
        {
            // When do I load?!
            _tokenProvider = tokenProvider;
            _refreshTokenService = refreshTokenService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var expiresAt = await _tokenProvider.GetExpiresAtAsync();
            if (expiresAt != null)
            {
                var isExpired = _refreshTokenService.CheckTokenIsExpired(expiresAt);
                if (isExpired)
                {
                    var refreshed = await _refreshTokenService.RefreshTokensAsync();
                    if (!refreshed)
                    {
                        await ClearAuthenticationStateAsync();
                    }
                }
            }

            return await base.GetAuthenticationStateAsync();
        }

        public async Task ClearAuthenticationStateAsync()
        {
            await _tokenProvider.SetTokensAsync(new Tokens(null, null, null, null));
            var state = new AuthenticationState(new ClaimsPrincipal());
            SetAuthenticationState(Task.FromResult(state));
        }
    }
}
