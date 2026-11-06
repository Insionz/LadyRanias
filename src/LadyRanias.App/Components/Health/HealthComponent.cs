using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LadyRanias.App.Components.Health;

public static class HealthComponent
{
    private const string ReadyTag = "ready";

    public static WebApplicationBuilder UseHealthComponent(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck<StorageHealthCheck>("storage", tags: [ReadyTag]);

        return builder;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });

        return app;
    }
}
