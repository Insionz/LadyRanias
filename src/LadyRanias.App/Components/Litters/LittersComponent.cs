using Azure.Data.Tables;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Litters.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.Litters;

public static class LittersComponent
{
    /// <summary>
    /// Krever at UseCatsComponent() også er kalt, fordi Litters bruker ICatHandler.
    /// </summary>
    public static WebApplicationBuilder UseLittersComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ILitterDatabase, TableLitterDatabase>();
        builder.Services.AddSingleton<IKittenDatabase, TableKittenDatabase>();
        builder.Services.AddSingleton<ILitterHandler, LitterHandler>();

        var connectionString = builder.Configuration.GetConnectionString("Storage")
            ?? throw new InvalidOperationException("ConnectionStrings:Storage mangler i konfigurasjonen");

        // Cats registrerer også en TableServiceClient. TryAdd gjør at vi bare får én, uansett rekkefølge.
        builder.Services.TryAddSingleton(new TableServiceClient(connectionString));

        return builder;
    }
}
