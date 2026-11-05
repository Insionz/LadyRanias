using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace LadyRanias.App.Components.Auth;

public static class AuthComponent
{
    public static WebApplicationBuilder UseAuthComponent(this WebApplicationBuilder builder)
    {
        var azureAd = builder.Configuration.GetSection("AzureAd");

        builder.Services
            .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(azureAd);

        return builder;
    }
}
