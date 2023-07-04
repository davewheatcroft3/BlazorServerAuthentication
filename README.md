# Blazor Sever OAuth/OpenID Authentication with Cookies
After spending countless hours of my life trying to get authentication working nicely with a none MS-based 
system (i.e. MS Identity/Azure) in Blazor Server and get it working with the admitedly nice looking features 
in Blazor (AuthenticationState, AuthorizedView, etc) and lots of Googling and lots of helpful articles its come to this!

None of those articles however had (in my opinion) a nice complete solution working out the box for my needs. So
Ive made this. See the sample project for a working version with an OAuth test application and the out of the box
Blazor server sample. You can sign up with a new user and then test the authorize setup with that user.

Im not sure I can reduce the steps required here any further (Im always wary of a Nuget package that requires a
million steps to get up and running!). Although 2 of the steps - adding AuthorizeView and adding the extended ErrorBoundary
I dont quite know why arent default in the Blazor sample project anyway.
All the complexity comes from having to handle the server side where HttpContext is available, and the scoped
SignalR runtime part where it isnt (or at least not reliably). Handling refreshing tokens and handling expired
tokens/stale cookies in a non-invasive and working way is tricky! So essentially we perform token chicks and 
check tokens in the _Host.cshtml (server side) and then pass down those tokens to the App.razor (SignalR runtime bit).
If we become unauthorized and we dont fully refresh our page, we have a token provider which keeps hold of your 
token in your scoped signal r session and allows you to keep using the app/http client. Then the next full
page refresh will update cookies and then pass back down to App.razor and start the process again.

NOTE (1): This library uses Cookies in the auth process largely because I think its the only feasible way
of storing your tokens. I've seen use of singleton arrays of user id and token pairings and you can potentially
add to what this library does and use local or session storage on the browser, but the issue here is youd have to
wait until the JS runtime is setup (OnAfterRender is currently easiest way).

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

3. Add your http clients in one of two ways:
```cs
builder.Services
    .AddBlazorServerAuthentication(builder.Configuration)
    .AddAuthorizedHttpClient<MyHttpClient>();
```
or
```cs
builder.AddHttpClient<MyHttpClient>().AddBlazorAuthenticationHandlers();
```

4. In your _Host.cshtml file add the following:
```cshtml
@inject IRefreshTokenService RefreshTokenService
...
@{
    ...
    var tokens = await RefreshTokenService.GetTokensCheckIfRefreshNeededAsync(HttpContext);
}
...
<component type="typeof(App)" param-Tokens="tokens" render-mode="ServerPrerendered" />
```

4. In your App.razor ensure you inherit from AppWithAuthentication
```razor
@inherits AppWithAuthentication
```

5. Add this into your appsettings.json:
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

### Recommended Additional Steps
These are listed as not required as strictly they arent, but you should really do these from a user experience standpoint:
1. Use the AuthorizeRouteView along with CascadingAuthenticationState and CascadingTokenExpiryState in your App.razor:
```razor
<CascadingAuthenticationState>
    <CascadingTokenExpiryState>
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
    </CascadingTokenExpiryState>
</CascadingAuthenticationState>
```
CascadingAuthenticationState and AuthorizeRouteView are in-built and recommended by Microsoft anyway for authentication, but the CascadingTokenExpiryState is
specific to this library to make available more information to handle refresh logic and token expiry.

2. Use AuthorizeViewConsideringTokenExpiry instead of AuthorizeView. This behaves the same way except also considers the refresh token/token expiry as well
as exposes a TokensExpired render fragment option.

```razor
<AuthorizeViewConsideringTokenExpiry>
    <Authorized>
        ...
    </Authorized>
    <TokensExpired>
        ...
    </TokensExpired>
    <NotAuthorized>
        ...
    </NotAuthorized>
</AuthorizeViewConsideringTokenExpiry>
```

3. If your http clients use EnsureSuccessfulStatusCode(), then your app will error and not gracefully tell the user to login again unless you handle it.
You can use the default ErrorBoundary and handle yourself, or you can use ConfigurableErrorBoundary.
Add this to your MainLayout.razor where you would usually see your @Body render fragment:

```razor
<ConfigurableErrorBoundary>
    <ChildContent>
        @Body
    </ChildContent>
    <UnauthorizedContent>
        @* Unauthorized responses from your authorized http clients will throw exception and direct us here.*@
        @* NOTE: Can not specify this and some ugly text with html button to login will show *@
    </UnauthorizedContent>
    <ErrorContent>
        @* Uncaught exceptions. NOTE: Can not specify this and some ugly default Blazor UI appears *@
    </ErrorContent>
</UnauthorizedErrorBoundary>
```

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
In App.razor in the NotAuthorized section, you could do the following (login service is automatically injected in AppWithAuthentication
and exposes LoginAsync and LogoutAsync methods:
```razor
<NotAuthorized>
    @{ LoginAsync(); }
</NotAuthorized>
```

For app wide auth use Authorize attribute in _Imports, or specific pages you want to auth - can also use AuthorizeView for parts of pages

