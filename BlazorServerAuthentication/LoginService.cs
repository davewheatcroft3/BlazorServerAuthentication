using BlazerServerAuthentication.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace BlazerServerAuthentication
{
    public interface ILoginService
    {
        Task LoginAsync();

        Task LoginAsync(string? returnUrl);

        Task LogoutAsync();
    }

    internal class LoginService : ILoginService
    {
        private readonly NavigationManager _navigationManager;
        private readonly BlazorServerAuthenticationSettings _settings;

        public LoginService(NavigationManager navigationManager, IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _navigationManager = navigationManager;
            _settings = settings.Value;
        }
        public async Task LoginAsync()
        {
            await LoginAsync(null);
        }

        public Task LoginAsync(string? returnUrl)
        {
            if (returnUrl != null)
            {
                var safeReturnUrl = Uri.EscapeDataString("/" + _navigationManager.ToBaseRelativePath(_navigationManager.Uri));
                _navigationManager.NavigateTo($"{_settings.GeneratedAppLoginRoute}?returnUrl={safeReturnUrl}", true);
            }
            else
            {
                _navigationManager.NavigateTo(_settings.GeneratedAppLoginRoute, true);
            }
            return Task.CompletedTask;
        }

        public Task LogoutAsync()
        {
            _navigationManager.NavigateTo(_settings.GeneratedAppLogoutRoute, true);
            return Task.CompletedTask;
        }
    }
}
