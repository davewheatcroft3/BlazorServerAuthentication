using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace BlazerServerAuthentication
{
    public class ConfigurableErrorBoundary : ErrorBoundary, IDisposable
    {
        [Inject]
        private TokenExpiryStateProvider TokenExpiryStateProvider { get; set; } = null!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = null!;

        [Parameter]
        public bool ClearErrorOnNavigation { get; set; } = true;

        [Parameter]
        public bool InterceptHttpStatusExceptionUnauthorized { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (ClearErrorOnNavigation)
            {
                NavigationManager.LocationChanged += NavigationChanged;
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (InterceptHttpStatusExceptionUnauthorized && CurrentException is HttpRequestException requestException)
            {
                if (requestException.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Recover();
                    TokenExpiryStateProvider.NotifyExpired();
                }
            }

            base.BuildRenderTree(builder);
        }

        private void NavigationChanged(object? sender, LocationChangedEventArgs e)
        {
            Recover();
        }

        public void Dispose()
        {
            if (ClearErrorOnNavigation)
            {
                NavigationManager.LocationChanged -= NavigationChanged;
            }
        }
    }
}
