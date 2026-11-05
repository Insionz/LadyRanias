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

        return builder;
    }
}
