namespace BlazerServerAuthentication
{
    internal class TokenExpiryStateProvider
    {
        public event TokenExpiryStateChangedHandler? TokenExpiryStateChanged;

        internal void NotifyExpired()
        {
            TokenExpiryStateChanged?.Invoke(new TokenExpiryState(true));
        }

        internal void NotifyRefreshed()
        {
            TokenExpiryStateChanged?.Invoke(new TokenExpiryState(false));
        }
    }

    internal delegate void TokenExpiryStateChangedHandler(TokenExpiryState state);

    public record TokenExpiryState(bool IsExpired);
}
