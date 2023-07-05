namespace BlazorServerAuthentication.Navigation
{
    internal record AuthenticationStatus(
        string? IdToken,
        string? AccessToken,
        string? RefreshToken,
        string? Email,
        DateTime? ExpiresAt);
}
