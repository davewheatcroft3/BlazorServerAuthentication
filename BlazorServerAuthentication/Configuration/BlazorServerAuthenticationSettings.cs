namespace BlazerServerAuthentication.Configuration
{
    public class BlazorServerAuthenticationSettings
    {
        public bool UseIdTokenForHttpAuthentication { get; set; } = false;

        public int RefreshExpiryClockSkewInMinutes { get; set; } = 5;

        public string GeneratedAppLoginRoute { get; set; } = "/Login";

        public string GeneratedAppLogoutRoute { get; set; } = "/Logout";

        //public bool CheckTokenExpiryOnNavigation { get; set; } = true;
    }
}
