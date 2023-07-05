using BlazerServerAuthentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorServerAuthentication.Navigation
{
    public class AuthenticationStatusDisplay : ComponentBase
    {
        private int _counter = 0;

        [Inject]
        internal RefreshTokenService RefreshTokenService { get; set; } = null!;

        protected override async void BuildRenderTree(RenderTreeBuilder builder)
        {
            var status = await RefreshTokenService.GetAuthenticationStatusAsync();

            if (status != null)
            {
                if (status.Email != null)
                {
                    AddTitleAndText(builder, "Email: ", status.Email);
                }

                AddTitleAndText(builder, "Id Token: ", status.IdToken);

                AddTitleAndText(builder, "Access Token: ", status.AccessToken);

                if (status.RefreshToken != null)
                {
                    AddTitleAndText(builder, "Refresh Token: ", status.RefreshToken);
                }

                if (status.ExpiresAt.HasValue)
                {
                    AddTitleAndText(
                        builder,
                        "Expires At UTC: ", 
                        $"{status.ExpiresAt.Value.ToShortDateString()} {status.ExpiresAt.Value.ToShortTimeString()}");
                }
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddMarkupContent(1, "Not Authenticated");
                builder.CloseElement();
            }

            base.BuildRenderTree(builder);
        }

        private void AddTitleAndText(RenderTreeBuilder builder, string title, string text)
        {
            builder.OpenElement(_counter++, "div");

            builder.OpenElement(_counter++, "b");
            builder.AddMarkupContent(_counter++, title);
            builder.CloseElement();

            builder.OpenElement(_counter++, "label");
            builder.AddMarkupContent(_counter++, text);
            builder.CloseElement();

            builder.CloseElement();

            builder.OpenElement(_counter++, "br");
            builder.CloseElement();
        }
    }
}
