using BlazerServerAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorServerAuthentication.Extensions
{
    public static class HttpContextExtensions
    {
        public static async Task ReadTokensFromCookie(this HttpContext httpContext)
        {
            var idToken = await httpContext.GetTokenAsync("id_token");
            var accessToken = await httpContext.GetTokenAsync("access_token");
            var refreshToken = await httpContext.GetTokenAsync("refresh_token");

            var tokenProvider = httpContext.RequestServices.GetRequiredService<ITokenProvider>();
            await tokenProvider.SetTokensAsync(httpContext.User, new Tokens(idToken, accessToken, refreshToken));
        }
    }
}
