using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazerServerAuthentication
{
    /// <summary>
    /// Authorize view will work - but it wont include if the token is expired.
    /// It also wouldnt know about if we can use refresh token.
    /// </summary>
    public class AuthorizeViewConsideringTokenExpiry : AuthorizeView
    {
        private AuthenticationState? _currentAuthenticationState;
        private TokenExpiryState? _tokenExpiredState;

        [CascadingParameter]
        private Task<AuthenticationState>? DefaultAuthenticationState { get; set; }

        [CascadingParameter]
        private TokenExpiryState? TokenExpiryState { get; set; }    
        
        /// <summary>
        /// The content that will be displayed if the user the users token has expired.
        /// If you dont set this and the token expires it will default to the NotAuthorized view.
        /// NOTE: the authentication state will still list the User.Identity.IsAuthenticated property as true...
        /// </summary>
        [Parameter] public RenderFragment? TokensExpired { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var isExpired = _tokenExpiredState?.IsExpired ?? false;
            if (isExpired)
            {
                if (TokensExpired != null)
                {
                    builder.AddContent(0, TokensExpired);
                }
                else
                {
                    builder.AddContent(0, NotAuthorized?.Invoke(_currentAuthenticationState!));
                }
            }
            else
            {
                base.BuildRenderTree(builder);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            _currentAuthenticationState = await DefaultAuthenticationState!;
            _tokenExpiredState = TokenExpiryState;

            await base.OnParametersSetAsync();
        }
    }
}
