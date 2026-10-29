using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.Cats;

public static class CatsComponent
{
    public static WebApplicationBuilder UseCatsComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICatDatabase, InMemoryCatDatabase>();
        builder.Services.AddSingleton<ICatHandler, CatHandler>();

        return builder;
    }
}