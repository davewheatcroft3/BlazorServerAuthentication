namespace BlazerServerAuthentication.Configuration
{
    public class OAuthSettings
    {
        public string ClientId { get; set; } = null!;

        public string MetadataAddress { get; set; } = null!;

        public bool RequireHttpsMetadata { get; set; }

        public string ResponseType { get; set; } = null!;

        public string CallbackPath { get; set; } = null!;

        public string SignOutPath { get; set; } = null!;

        public string TokenUrl { get; set; } = null!;

        public string LogoutUrl { get; set; } = null!;

        public string ClientSecret { get; set; } = null!;
    }
}
