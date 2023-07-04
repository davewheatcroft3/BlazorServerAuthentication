using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazerServerAuthentication
{
    public class CascadingTokenExpiryState : ComponentBase, IDisposable
    {
        [Inject]
        private TokenExpiryStateProvider StateProvider { get; set; } = null!;

        [Inject]
        private RefreshTokenService RefreshTokenService { get; set; } = null!;

        [Inject]
        private ITokenProvider TokenProvider { get; set; } = null!;

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        private TokenExpiryState? _tokenExpiryState;

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            __builder.OpenComponent(0, typeof(CascadingValue<TokenExpiryState>));
            __builder.AddAttribute(1, nameof(CascadingValue<TokenExpiryState>.Value), _tokenExpiryState);
            __builder.AddAttribute(2, nameof(CascadingValue<TokenExpiryState>.ChildContent), ChildContent);
            __builder.CloseComponent();

            base.BuildRenderTree(__builder);
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            StateProvider.TokenExpiryStateChanged += OnAuthenticationStateChanged;

            var idToken = TokenProvider.GetIdTokenAsync().Result;
            var accessToken = TokenProvider.GetAccessTokenAsync().Result;
            var refreshToken = TokenProvider.GetRefreshTokenAsync().Result;
            var expiresAt = TokenProvider.GetExpiresAtAsync().Result;

            if (idToken != null && accessToken != null && refreshToken != null && expiresAt != null)
            {
                var expired = RefreshTokenService.CheckTokenIsExpired(expiresAt);
                _tokenExpiryState = new TokenExpiryState(expired);
            }
        }

        private void OnAuthenticationStateChanged(TokenExpiryState state)
        {
            if (_tokenExpiryState?.IsExpired != state.IsExpired)
            {
                _ = InvokeAsync(() =>
                {
                    _tokenExpiryState = state;
                    StateHasChanged();
                });
            }
        }

        public void Dispose()
        {
            StateProvider.TokenExpiryStateChanged -= OnAuthenticationStateChanged;
        }
    }
}
