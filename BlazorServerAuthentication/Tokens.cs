namespace BlazerServerAuthentication
{
   public record Tokens(string? IdToken, string? AccessToken, string? RefreshToken,string? ExpiresAt);
}
