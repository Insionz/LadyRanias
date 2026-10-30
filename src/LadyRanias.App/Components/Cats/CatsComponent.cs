using Azure.Data.Tables;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.Cats;

public static class CatsComponent
{
    public static WebApplicationBuilder UseCatsComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICatDatabase, TableCatDatabase>();
        builder.Services.AddSingleton<ICatHandler, CatHandler>();

        var connectionString = builder.Configuration.GetConnectionString("Storage") 
            ?? throw new InvalidOperationException("ConnectionStrings:Storage mangler i konfigurasjonen");

        builder.Services.AddSingleton(new TableServiceClient(connectionString));

        return builder;
    }
}