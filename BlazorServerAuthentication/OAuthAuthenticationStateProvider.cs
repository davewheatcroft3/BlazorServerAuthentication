using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace BlazerServerAuthentication
{
    public class OAuthAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
    {
        private readonly TokenProvider _tokenProvider;

        private readonly RefreshTokenService _refreshTokenService;

        internal OAuthAuthenticationStateProvider(TokenProvider tokenProvider, RefreshTokenService refreshTokenService)
        {
            _tokenProvider = tokenProvider;
            _refreshTokenService = refreshTokenService;

            _tokenProvider.TokensChanged += TokensChanged;
            _tokenProvider.TokensCleared += TokensCleared;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (await _refreshTokenService.CheckIfRefreshNeededAsync())
            {
                var refreshed = await _refreshTokenService.RefreshTokensAsync();
                if (!refreshed)
                {
                    return await ClearAuthenticationStateAsync();
                }
            }
          
            var state = await base.GetAuthenticationStateAsync();
            return state;
        }

        public async Task<AuthenticationState> ClearAuthenticationStateAsync()
        {
            var state = await GetAuthenticationStateAsync();
            await _tokenProvider.ClearTokensAsync(state.User);
            var anonymous = new AuthenticationState(new ClaimsPrincipal());
            SetAuthenticationState(Task.FromResult(anonymous));
            return state;
        }

        public void Dispose()
        {
            _tokenProvider.TokensChanged -= TokensChanged;
            _tokenProvider.TokensCleared -= TokensCleared;
        }

        private void TokensChanged(object? sender, EventArgs e)
        {
            // Do anything?
        }

        private async void TokensCleared(object? sender, EventArgs e)
        {
            await ClearAuthenticationStateAsync();
        }
    }
}
