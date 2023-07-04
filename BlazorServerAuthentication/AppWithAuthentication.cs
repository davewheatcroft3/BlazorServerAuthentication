using Microsoft.AspNetCore.Components;

namespace BlazerServerAuthentication
{
    public class AppWithAuthentication : ComponentBase
    {
        [Inject]
        private ITokenProvider TokenProvider { get; set; } = null!;

        [Inject]
        private ILoginService LoginService { get; set; } = null!;

        [Parameter]
        public Tokens Tokens { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await TokenProvider.SetTokensAsync(Tokens);

            await base.OnInitializedAsync();
        }

        public async Task LoginAsync()
        {
            await LoginService.LoginAsync();
        }

        public async Task LogoutAsync()
        {
            await LoginService.LoginAsync();
        }
    }
}
