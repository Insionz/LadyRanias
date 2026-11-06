using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace LadyRanias.App.Components.Telemetry;

/// <summary>
/// Sender forespørsler, feil, kall til Azure (Storage, e-post) og loggene fra ILogger til Application Insights.
/// Slås bare på når APPLICATIONINSIGHTS_CONNECTION_STRING er satt, altså i Azure.
/// Lokalt, i testene og i CI skjer det ingenting.
/// </summary>
public static class TelemetryComponent
{
    public static WebApplicationBuilder UseTelemetryComponent(this WebApplicationBuilder builder)
    {
        if (string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            return builder;
        }

        // Leser koblingsstrengen og OTEL_SERVICE_NAME fra miljøvariablene selv.
        builder.Services.AddOpenTelemetry().UseAzureMonitor();

        // Azure spør /health/... flere ganger i minuttet (probene fra Del 1).
        // De kallene er ikke interessante, og ville druknet de ekte besøkene og kostet penger.
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health"));

        return builder;
    }
}
