namespace BlazerServerAuthentication
{
    internal interface ITokenProvider
    {
        Task<string?> GetIdTokenAsync();

        Task<string?> GetAccessTokenAsync();

        Task<string?> GetRefreshTokenAsync();

        Task<string?> GetExpiresAtAsync();

        Task SetTokensAsync(Tokens tokens);
    }

    internal class TokenProvider : ITokenProvider
    {
        private static string? _idToken;
        private static string? _accessToken;
        private static string? _refreshToken;
        private static string? _expiresAt;

        public Task<string?> GetIdTokenAsync()
        {
            return Task.FromResult(_idToken);
        }

        public Task<string?> GetAccessTokenAsync()
        {
            return Task.FromResult(_accessToken);
        }

        public Task<string?> GetRefreshTokenAsync()
        {
            return Task.FromResult(_refreshToken);
        }

        public Task<string?> GetExpiresAtAsync()
        {
            return Task.FromResult(_expiresAt);
        }

        public Task SetTokensAsync(Tokens? tokens)
        {
            _idToken = tokens?.IdToken;
            _accessToken = tokens?.AccessToken;
            _refreshToken = tokens?.RefreshToken;
            _expiresAt = tokens?.ExpiresAt;
            return Task.CompletedTask;
        }
    }
}
