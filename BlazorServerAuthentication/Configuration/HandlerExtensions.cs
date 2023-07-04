using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace BlazerServerAuthentication
{
    public interface IHttpClientWithAuthenticationBuilder
    {
        IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient(string name, Action<IServiceProvider, HttpClient> action);

        IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient<T>()
            where T : class;

        IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient<T>(Action<IServiceProvider, HttpClient> action)
            where T : class;
    }

    internal class HttpClientWithAuthenticationBuilder : IHttpClientWithAuthenticationBuilder
    {
        private readonly IServiceCollection _services;

        public HttpClientWithAuthenticationBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient(string name, Action<IServiceProvider, HttpClient> action)
        {
            _services
                .AddHttpClient(name, action)
                .AddBlazorServerAuthenticationHandlers();
            return this;
        }

        public IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient<T>()
            where T : class
        {
            _services
                .AddHttpClient<T>((sp, h) => { })
                .AddBlazorServerAuthenticationHandlers();
            return this;
        }

        public IHttpClientWithAuthenticationBuilder AddAuthorizedHttpClient<T>(Action<IServiceProvider, HttpClient> action)
            where T : class
        {
            _services
                .AddHttpClient<T>(action)
                .AddBlazorServerAuthenticationHandlers();
            return this;
        }
    }
}
