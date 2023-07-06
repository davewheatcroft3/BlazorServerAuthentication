using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BlazerServerAuthentication
{
    public class RecoverTokensFromCookiesMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var idToken = await context.GetTokenAsync("id_token");
            var accessToken = await context.GetTokenAsync("access_token");
            var refreshToken = await context.GetTokenAsync("refresh_token");

            var tokenProvider = context.RequestServices.GetRequiredService<ITokenProvider>();
            await tokenProvider.SetTokensAsync(context.User, new Tokens(idToken, accessToken, refreshToken));

            await next(context);
        }
    }
}