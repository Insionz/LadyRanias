using Azure.Data.Tables;
using Azure.Storage.Blobs;
using LadyRanias.App.Components.CatImages.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.CatImages;

public static class CatImagesComponent
{
    public static WebApplicationBuilder UseCatImagesComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IImageStore, BlobImageStore>();

        var connectionString = builder.Configuration.GetConnectionString("Storage")
            ?? throw new InvalidOperationException("ConnectionStrings:Storage mangler i konfigurasjonen");

        builder.Services.AddSingleton(new BlobServiceClient(connectionString));

        return builder;
    }
}
