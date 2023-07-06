# Blazor Sever OAuth/OpenID Authentication with Cookies
After spending countless hours of my life trying to get authentication working nicely with a none MS-based system (i.e. MS Identity/Azure) in Blazor Server and get it working with the admitedly nice looking features in Blazor (AuthenticationState, AuthorizedView, etc) and lots of Googling and lots of helpful articles its come to this!

None of those articles however had (in my opinion) a nice complete solution working out the box for my needs. So Ive made this. See the sample project for a working version with an OAuth test application and the out of the box Blazor server sample. You can sign up with a new user and then test the authorize setup with that user or use your Google account.
If you encounter any issues that might be related to your OAuth server setup, this is a great way to test everything works:

https://openidconnect.net/

This was a great reference for helping simplify my initial setup:

https://github.com/DuendeSoftware/Duende.AccessTokenManagement/wiki/blazor-server

However the complexity and the additional boilerplate was still too much for me, so I took some if its implementation ideas and used them to simplify my approach and reduce the required steps;

All the complexity comes from having to handle the server side where HttpContext is available, and the scoped SignalR runtime part where it isnt (or at least not reliably). Handling refreshing tokens and handling expired tokens/stale cookies in a non-invasive and working way is tricky!
So essentially we perform token checks and store the results in the Oidc OnTokenValidated event (server side) and then pass down those tokens to SignalR scoped runtime bit via a singleton service which stores tokens against the users unique circuit id.

NOTE: I believe in .NET 8 Authentication is being re-vamped to not rely on HttpContext, which will likely make most
of this redundant, but until then!

## Installation

### Required Steps
1. Install Nuget package
```
Install-Package BlazorServer.Authentication
```

2. In your Program.cs add
```cs
builder.Services.AddBlazorServerAuthentication(builder.Configuration);
```
And
```cs
app.UseBlazorServerAuthentication();
```

3. Ensure your http clients authenticate by injecting the IHttpClientAuthenticator in this library.
Then use as follows before you http calls:
```cs
await _httpClientAuthenticator.PrepareHttpClientAsync(_httpClient);
```
(NOTE: Http message handlers CANNOT be used due to the way DI works with Blazor Server)

4. Add this into your appsettings.json:
```json
"Authentication": {
    "OAuth": {
      "ClientId": "<auth provider client id>",
      "ClientSecret": "<auth provider client secret>",
      "MetadataAddress": "<something like: auth provider url>/.well-known/openid-configuration",
      "Domain": "<auth provider url>",
      "TokenUrl": "<something like: auth provider url>/oauth2/token",
      "RequireHttpsMetadata": false, // Can toggle based on whether in development or production
      "ResponseType": "<oauth grant type>",
      "CallbackPath": "<relative route for login complete callback>",
      "SignOutUrl": "<relative route for sign out callback 9optional)>"
    }
  }
```
You dont have to have this in your app.settings, but if you dont ensure you set this in the 
AddBlazorServerAuthentication call like below:
NOTE: all the properties are required.
```cs
services.AddBlazorServerAuthentication(configuration, options =>
{
    @* Set my options for this library *@
}, oAuthSettings =>
{
    @* Set my oAuth properties from your auth token provider - all required*@
});
```

5. In your App.razor ensure you use CascadingAuthenticationState and AuthorizeRouteView
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                @* You are not authorized! *@
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p role="alert">Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

## Optional Additional Steps
You can also add into your App.razor underneath the AuthorizeRouteView (in the Found render fragment):
```razor
<AuthenticateOnNavigation></AuthenticateOnNavigation>
```
If you would like to check token validity when navigating between pages. It will try and refresh the token
or if not put the app into the not authorized state if each time the route changes if present.

## Additional Information
You can configure some parts of the authentication in AddBlazorServerAuthentication method:
The default properties are shown as being set.
```cs
services.AddBlazorServerAuthentication(configuration, options =>
{
    options.UseIdTokenForHttpAuthentication = false;
    options.RefreshExpiryClockSkewInMinutes = 5
    options.GeneratedAppLoginRoute = "/Login";
    options.GeneratedAppLogoutRoute = "/Logout";
})
```

You can inject ILoginService at any point to manually start the OAuth flow for logging in or out.

For app wide auth use Authorize attribute in _Imports, or specific pages you want to auth - can also use AuthorizeView for parts of pages

