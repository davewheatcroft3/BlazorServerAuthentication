using BlazerServerAuthentication.Configuration;
using BlazerServerAuthentication.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazerServerAuthentication
{
    public static class DependencyInjection
    {
        public static IHttpClientWithAuthenticationBuilder AddBlazorServerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddBlazorServerAuthentication(services, configuration, null, null);
        }

        public static IHttpClientWithAuthenticationBuilder AddBlazorServerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<BlazorServerAuthenticationSettings> options)
        {
            return AddBlazorServerAuthentication(services, configuration, options, null);
        }

        public static IHttpClientWithAuthenticationBuilder AddBlazorServerAuthentication(this IServiceCollection services,
            IConfiguration configuration,
            Action<BlazorServerAuthenticationSettings>? options,
            Action<OAuthSettings>? oAuthOptions)
        {
            services.AddAuthenticationCore();
            services.AddAuthorizationCore();

            // TODO: setting for whether to use refresh token? (need refresh token service change + option in settings)

            services.Configure<BlazorServerAuthenticationSettings>(x =>
            {
                if (options != null)
                {
                    options(x);
                }
            });

            var settings = new OAuthSettings();

            var oAuthSection = configuration.GetSection("Authentication:OAuth");
            if (oAuthSection != null)
            {
                configuration.GetSection("Authentication:OAuth").Bind(settings);
            }

            if (oAuthOptions != null)
            {
                oAuthOptions(settings);
            }

            services.Configure<OAuthSettings>(configuration.GetSection("Authentication:OAuth"));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddOpenIdConnect(options =>
                {
                    options.ResponseType = settings.ResponseType;
                    options.MetadataAddress = settings.MetadataAddress;
                    options.ClientId = settings.ClientId;
                    options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProviderForSignOut = OnRedirectToIdentityProviderForSignOut
                    };

                    options.ClientSecret = settings.ClientSecret;
                });

            services.AddScoped<ITokenProvider, TokenProvider>();
            services.AddScoped<RefreshTokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<ILoginService, LoginService>();

            services.AddScoped<OAuthAuthenticationStateProvider>(sp =>
            {
                var tokenProvider = sp.GetRequiredService<ITokenProvider>();
                var refreshTokenService = sp.GetRequiredService<RefreshTokenService>();
                return new OAuthAuthenticationStateProvider(tokenProvider, refreshTokenService);
            });
            services.AddScoped<AuthenticationStateProvider>(sp =>
            {
                var tokenProvider = sp.GetRequiredService<ITokenProvider>();
                var refreshTokenService = sp.GetRequiredService<RefreshTokenService>();
                return new OAuthAuthenticationStateProvider(tokenProvider, refreshTokenService);
            });

            services.AddScoped<HttpAccessTokenHandler>();
            services.AddScoped<HttpRefreshTokenTokenHandler>();

            return new HttpClientWithAuthenticationBuilder(services);
        }

        public static IHttpClientBuilder AddBlazorServerAuthenticationHandlers(this IHttpClientBuilder httpClientBuilder)
        {
            return httpClientBuilder
                .AddHttpMessageHandler<HttpAccessTokenHandler>()
                .AddHttpMessageHandler<HttpRefreshTokenTokenHandler>();
        }

        public static void UseBlazorServerAuthentication(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<IOptions<BlazorServerAuthenticationSettings>>();

            app.MapGet(options.Value.GeneratedAppLoginRoute, async (HttpContext context, IOptions<OAuthSettings> options) =>
            {
                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
                {
                    RedirectUri = options.Value.CallbackPath
                });
            });

            app.MapGet(options.Value.GeneratedAppLogoutRoute, async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            });

            /*if (options.Value.CheckTokenExpiryOnNavigation)
            {
                var navigationManager = app.Services.GetRequiredService<NavigationManager>();
                var refreshTokenService = app.Services.GetRequiredService<RefreshTokenService>();
                var tokenProvider = app.Services.GetRequiredService<ITokenProvider>();
                navigationManager.LocationChanged += async (o, e) =>
                {
                    var expiresAt = await tokenProvider.GetExpiresAtAsync();
                    if (expiresAt != null)
                    {
                        if (refreshTokenService.CheckTokenIsExpired(expiresAt))
                        {
                            // Login here... clear authentication state? Call login service LoginAsync?
                            // Should we just check with oauth provider? Only problem there is round trip call
                            // (maybe provide enum option? NONE, EVERY_PAGE_CHECK_WITH_AUTH, ONLY_WHEN_EXPIRED)
                        }
                    }
                };
            }*/
        }

        private static Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            var settings = context.HttpContext.RequestServices.GetRequiredService<IOptions<OAuthSettings>>().Value;

            var logoutUrl = $"{context.Request.Scheme}://{context.Request.Host}{settings.CallbackPath}";

            context.ProtocolMessage.IssuerAddress = $"{settings.Domain}/logout";
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
}
