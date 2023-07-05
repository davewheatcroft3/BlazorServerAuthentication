using BlazerServerAuthentication;
using BlazerServerAuthentication.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorServer.Events;

public class OidcEvents : OpenIdConnectEvents
{
    private readonly ITokenProvider _tokenProvider;

    public OidcEvents(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;

        OnRedirectToIdentityProviderForSignOut = CustomRedirectToIdentityProviderForSignOut;
    }
    
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        await _tokenProvider.SetTokensAsync(context.Principal!, new Tokens(
            context.TokenEndpointResponse?.IdToken,
            context.TokenEndpointResponse?.AccessToken,
            context.TokenEndpointResponse?.RefreshToken));
        
        await base.TokenValidated(context);
    }

    private Task CustomRedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        var settings = context.HttpContext.RequestServices.GetRequiredService<IOptions<OAuthSettings>>().Value;

        var logoutUrl = $"{context.Request.Scheme}://{context.Request.Host}{settings.SignOutPath}";

        context.ProtocolMessage.IssuerAddress = settings.LogoutUrl;
        context.ProtocolMessage.SetParameter("client_id", settings.ClientId);
        context.ProtocolMessage.SetParameter("logout_uri", logoutUrl);
        context.ProtocolMessage.SetParameter("redirect_uri", logoutUrl);
        context.ProtocolMessage.Scope = "openid";
        context.ProtocolMessage.ResponseType = settings.ResponseType;

        context.Properties.Items.Remove(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Properties.Items.Remove(OpenIdConnectDefaults.AuthenticationScheme);

        return Task.CompletedTask;
    }
}