using Microsoft.AspNetCore.Authentication.Cookies;
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

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

        builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            var existing = options.Events.OnRedirectToIdentityProvider;
            options.Events.OnRedirectToIdentityProvider = async context =>
            {
                var path = context.Request.Path;

                if (path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/auth/login"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.HandleResponse();
                    return;
                }

                await existing(context);
            };
        });

        builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            var existing = options.Events.OnRedirectToAccessDenied;
            options.Events.OnRedirectToAccessDenied = async context =>
            {
                var path = context.Request.Path;

                if (path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                await existing(context);
            };
        });

        return builder;
    }
}
