using BlazerServerAuthentication.Configuration;
using BlazorServer.Events;
using BlazorServerAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazerServerAuthentication
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBlazorServerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddBlazorServerAuthentication(services, configuration, null, null);
        }

        public static IServiceCollection AddBlazorServerAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<BlazorServerAuthenticationSettings> options)
        {
            return AddBlazorServerAuthentication(services, configuration, options, null);
        }

        public static IServiceCollection AddBlazorServerAuthentication(this IServiceCollection services,
            IConfiguration configuration,
            Action<BlazorServerAuthenticationSettings>? options,
            Action<OAuthSettings>? oAuthOptions)
        {
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
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.EventsType = typeof(CookieEvents);
                })
                .AddOpenIdConnect(options =>
                {
                    options.ResponseType = settings.ResponseType;
                    options.Authority = settings.Authority;
                    options.MetadataAddress = settings.MetadataAddress;
                    options.ClientId = settings.ClientId;
                    options.ClientSecret = settings.ClientSecret;
                    options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.EventsType = typeof(OidcEvents);

                    options.MapInboundClaims = false;
                    options.ResponseMode = "query";
                });
            
            services.AddScoped<JwtService>();
            services.AddScoped<RefreshTokenService>();
            services.AddSingleton<ITokenProvider, TokenProvider>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IHttpClientAuthenticator>(sp =>
            {
                var refreshTokenService = sp.GetService<RefreshTokenService>()!;
                return new HttpClientAuthenticator(refreshTokenService);
            });

            services.AddScoped<CookieEvents>();
            services.AddScoped<OidcEvents>();

            services.AddTransient<RecoverTokensFromCookiesMiddleware>();

            return services;
        }

        public static void UseBlazorServerAuthentication(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<IOptions<BlazorServerAuthenticationSettings>>();

            app.MapGet(options.Value.GeneratedAppLoginRoute, async (string? returnUrl, HttpContext context) =>
            {
                string redirectUri = "/";
                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    redirectUri = returnUrl;
                }

                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
                {
                    RedirectUri = redirectUri
                });
            });

            app.MapGet(options.Value.GeneratedAppLogoutRoute, async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            });

            app.UseMiddleware<RecoverTokensFromCookiesMiddleware>();
        }
    }
}
