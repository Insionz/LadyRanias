using Azure.Data.Tables;
using Azure.Communication.Email;
using Azure.Storage.Queues;
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

    private static readonly string[] RequiredEmailSettings = ["Email:ConnectionString", "Email:SenderAddress", "Email:NotifyAddress"];

    public static WebApplicationBuilder UseContactComponent(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IContactDatabase, TableContactDatabase>();
        builder.Services.AddSingleton<IContactHandler, ContactHandler>();
        builder.Services.AddSingleton<INotificationQueue, StorageNotificationQueue>();
        builder.UseEmailSender();

        // Bakgrunnsjobben som sender e-postvarsler. Starter og stopper sammen med appen.
        builder.Services.AddHostedService<ContactNotificationWorker>();

        var connectionString = builder.Configuration.GetConnectionString("Storage")
            ?? throw new InvalidOperationException("ConnectionStrings:Storage mangler i konfigurasjonen");

        // Cats registrerer også en TableServiceClient. TryAdd gjør at det bare blir én.
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton(new TableServiceClient(connectionString));
        builder.Services.TryAddSingleton(new QueueServiceClient(connectionString));

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

    /// <summary>
    /// Ekte e-post via Azure Communication Services hvis den er satt opp, ellers bare logging.
    /// Samme mønster som innloggingen i steg 8: utvikling, tester og CI virker uten Azure,
    /// men i produksjon skal appen stoppe med en gang hvis noe mangler.
    /// </summary>
    private static void UseEmailSender(this WebApplicationBuilder builder)
    {
        var emailConnectionString = builder.Configuration["Email:ConnectionString"];

        if (builder.Environment.IsProduction())
        {
            foreach (var key in RequiredEmailSettings)
            {
                if (string.IsNullOrWhiteSpace(builder.Configuration[key]))
                {
                    throw new InvalidOperationException($"{key} mangler i konfigurasjonen. E-postvarsler kan ikke sendes.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(emailConnectionString))
        {
            builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
        }
        else
        {
            builder.Services.AddSingleton(new EmailClient(emailConnectionString));
            builder.Services.AddSingleton<IEmailSender, AcsEmailSender>();
        }
    }
}
