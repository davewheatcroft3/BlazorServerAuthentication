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

        public event EventHandler? TokensChanged;
        public event EventHandler? TokensCleared;

        public TokenProvider(IOptions<BlazorServerAuthenticationSettings> settings)
        {
            _settings = settings.Value;
        }

        public Task<Tokens?> GetTokensAsync(ClaimsPrincipal user)
        {
            var sub = user.FindFirst(_settings.UserIdentifierClaimName)?.Value;

            if (sub != null)
            {
                if (_tokens.TryGetValue(sub, out var value))
                {
                    return Task.FromResult<Tokens?>(value);
                }
            }

            return Task.FromResult<Tokens?>(null);
        }

        public Task SetTokensAsync(ClaimsPrincipal user, Tokens tokens)
        {
            var sub = user.FindFirst(_settings.UserIdentifierClaimName)?.Value
                ?? throw new InvalidOperationException($"No {_settings.UserIdentifierClaimName} claim");
            _tokens[sub] = tokens;

            TokensChanged?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        public Task ClearTokensAsync(ClaimsPrincipal user)
        {
            var sub = user.FindFirst(_settings.UserIdentifierClaimName)?.Value
                ?? throw new InvalidOperationException($"No {_settings.UserIdentifierClaimName} claim");
            _tokens.Remove(sub, out _);

            TokensCleared?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }
    }
}
