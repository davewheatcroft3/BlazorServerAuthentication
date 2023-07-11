using BlazerServerAuthentication.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BlazerServerAuthentication
{
    public record Tokens(string? IdToken, string? AccessToken, string? RefreshToken);

    public interface ITokenProvider
    {
        Task<Tokens?> GetTokensAsync(ClaimsPrincipal user);

        Task SetTokensAsync(ClaimsPrincipal user, Tokens tokens);

        Task ClearTokensAsync(ClaimsPrincipal user);
    }

    internal class TokenProvider : ITokenProvider
    {
        private readonly BlazorServerAuthenticationSettings _settings;

        private readonly ConcurrentDictionary<string, Tokens> _tokens = new();

        public TokenProvider(IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _settings = settings.Value;
        }

        public Task<Tokens?> GetTokensAsync(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(_settings.UserIdentifierClaimName)?.Value;
            ThrowIfClaimsPresentIdClaimNull(user, idClaim);

            if (idClaim != null)
            {
                if (_tokens.TryGetValue(idClaim!, out var value))
                {
                    return Task.FromResult<Tokens?>(value);
                }
            }
           
            return Task.FromResult<Tokens?>(null);
        }

        public Task SetTokensAsync(ClaimsPrincipal user, Tokens tokens)
        {
            var idClaim = user.FindFirst(_settings.UserIdentifierClaimName)?.Value;
            ThrowIfClaimsPresentIdClaimNull(user, idClaim);

            if (idClaim != null)
            {
                _tokens[idClaim] = tokens;
            }

            return Task.CompletedTask;
        }

        public Task ClearTokensAsync(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(_settings.UserIdentifierClaimName)?.Value;
            ThrowIfClaimsPresentIdClaimNull(user, idClaim);

            if (idClaim != null)
            {
                _tokens.Remove(idClaim!, out _);
            }

            return Task.CompletedTask;
        }

        private void ThrowIfClaimsPresentIdClaimNull(ClaimsPrincipal user, string? idClaim)
        {
            if (user.Claims.Any() && idClaim == null)
            {
                throw new Exception(
                    $"No claim found matching the name {_settings.UserIdentifierClaimName} given in the OAuthSettings.UserIdentifierClaimName property." +
                    $" You must ensure this is a valid claim key available in your given tokens. Because Blazor Server has no reliable of storing" +
                    $"user data (local storage or cookies have downsides) we use this to keep hold of user data on the running Blazor server instance.");
            }
        }
    }
}
