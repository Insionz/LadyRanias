using Azure.Data.Tables;
using LadyRanias.App.Components.Contact.Api;
using LadyRanias.App.Components.Contact.Impl;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.RateLimiting;

namespace LadyRanias.App.Components.Contact;

public static class ContactComponent
{
    /// <summary>Navnet controlleren bruker i [EnableRateLimiting(...)].</summary>
    public const string RateLimitPolicy = "contact";

    public static WebApplicationBuilder UseContactComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IContactDatabase, TableContactDatabase>();
        builder.Services.AddSingleton<IContactHandler, ContactHandler>();

        var connectionString = builder.Configuration.GetConnectionString("Storage")
            ?? throw new InvalidOperationException("ConnectionStrings:Storage mangler i konfigurasjonen");

        // Cats registrerer også en TableServiceClient. TryAdd gjør at det bare blir én.
        builder.Services.TryAddSingleton(new TableServiceClient(connectionString));

        // Rate limiting: maks N meldinger per time per IP-adresse. Standard er 5.
        // Kan overstyres i konfigurasjonen (testene gjør det, så de ikke går tom for kvote).
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicy, httpContext =>
            {
                // Grensen leses når forespørselen kommer, ikke ved oppstart. Da ser den også
                // innstillinger som testene legger til etter at Program.cs har kjørt.
                var permitLimit = httpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    .GetValue("Contact:RateLimit:PermitLimit", 5);

                return RateLimitPartition.GetFixedWindowLimiter(
                    // Hver IP-adresse får sin egen kvote. Bak en proxy er dette den ekte adressen,
                    // takket være UseForwardedHeaders (X-Forwarded-For) fra steg 8.
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0, // ikke sett forespørsler i kø, avvis med en gang
                    });
            });
        });

        return builder;
    }
}
