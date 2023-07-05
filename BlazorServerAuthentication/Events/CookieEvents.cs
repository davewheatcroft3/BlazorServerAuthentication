using BlazerServerAuthentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlazorServer.Events;

internal class CookieEvents : CookieAuthenticationEvents
{
    private readonly RefreshTokenService _refreshTokenService;

    public CookieEvents(RefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }
    
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (await _refreshTokenService.CheckIfRefreshNeededAsync(context.HttpContext.User))
        {
            var refreshed = await _refreshTokenService.RefreshTokensAsync(context.HttpContext.User);
            if (!refreshed)
            {
                context.RejectPrincipal();
            }
            else
            {
                context.ShouldRenew = true;
            }
        }
        
        await base.ValidatePrincipal(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await _refreshTokenService.RevokeRefreshTokenAsync(context.HttpContext.User);

        await base.SigningOut(context);
    }
}