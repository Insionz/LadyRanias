using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace LadyRanias.App.Components.Auth;

public static class AuthComponent
{
    public static WebApplicationBuilder UseAuthComponent(this WebApplicationBuilder builder)
    {
        var azureAd = builder.Configuration.GetSection("AzureAd");

        if (string.IsNullOrEmpty(azureAd["ClientId"]))
        {
            // Uten Entra-oppsett (CI og tester har ikke user secrets).
            // I produksjon er det en feil: da skal appen stoppe med en gang, ikke kjøre uten innlogging.
            if (builder.Environment.IsProduction())
            {
                throw new InvalidOperationException("AzureAd:ClientId mangler i konfigurasjonen. Innlogging kan ikke settes opp.");
            }

            // Ellers: bare cookie, uten Entra. Ingen kan logge inn, så api/admin gir alltid 401.
            // Testene bytter dette ut med TestAuthHandler.
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();
        }
        else
        {
            builder.Services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(azureAd);
        }

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

        // Ikke innlogget, med Entra: OpenIdConnect vil sende til Entra. For api/... svarer vi 401 i stedet.
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
            // Ikke innlogget, uten Entra: cookien vil sende til en innloggingsside. For api/... svarer vi 401.
            var existingLogin = options.Events.OnRedirectToLogin;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                return existingLogin(context);
            };

            // Innlogget, men uten rollen: 403 i stedet for omdirigering til «Access denied».
            var existingDenied = options.Events.OnRedirectToAccessDenied;
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                return existingDenied(context);
            };
        });

        return builder;
    }
}
