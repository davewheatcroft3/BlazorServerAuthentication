using BlazerServerAuthentication.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace BlazerServerAuthentication.Handlers
{
    internal class HttpAccessTokenHandler : DelegatingHandler
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly BlazorServerAuthenticationSettings _settings;

        public HttpAccessTokenHandler(ITokenProvider tokenProvider, IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _tokenProvider = tokenProvider;
            _settings = settings.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _settings.UseIdTokenForHttpAuthentication
                ? await _tokenProvider.GetIdTokenAsync()
                : await _tokenProvider.GetAccessTokenAsync();

            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
