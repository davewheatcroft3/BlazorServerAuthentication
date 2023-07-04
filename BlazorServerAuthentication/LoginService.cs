using Microsoft.AspNetCore.Components;

namespace BlazerServerAuthentication
{
    public interface ILoginService
    {
        Task LoginAsync();

        Task LogoutAsync();
    }

    internal class LoginService : ILoginService
    {
        private readonly NavigationManager _navigationManager;

        public LoginService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public Task LoginAsync()
        {
            _navigationManager.NavigateTo("/Login", true);
            return Task.CompletedTask;
        }

        public Task LogoutAsync()
        {
            _navigationManager.NavigateTo("/Logout", true);
            return Task.CompletedTask;
        }
    }
}
