namespace BlazorServerAuthentication.Navigation
{
    internal record AuthenticationStatus(
        string IdToken,
        string AccessToken,
        string RefreshToken,
        string Name,
        DateTime? ExpiresAt);
}
