using BlazerServerAuthentication;
using Microsoft.AspNetCore.Components;

namespace BlazorServerAuthentication.Navigation
{
    public class AuthenticateOnNavigation : ComponentBase, IDisposable
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;

        [Inject]
        internal RefreshTokenService RefreshTokenService { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            if (await RefreshTokenService.CheckIfRefreshNeededAsync())
            {
                await RefreshTokenService.RefreshTokensAsync();
            }

            NavigationManager.LocationChanged += LocationChanged;

            await base.OnInitializedAsync();
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= LocationChanged;
        }

        private async void LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (await RefreshTokenService.CheckIfRefreshNeededAsync())
            {
                await RefreshTokenService.RefreshTokensAsync();
            }
        }
    }
}
