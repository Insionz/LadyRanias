using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.CatImages.Impl;
using LadyRanias.App.Components.Cats.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.CatImages;

public static class CatImagesComponent
{
    public static WebApplicationBuilder UseCatImagesComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IImageStore, BlobImageStore>();
        builder.Services.AddSingleton<IImageProcessor, SkiaSharpImageProcessor>();
        builder.Services.AddSingleton<ICatImageDatabase, TableCatImageDatabase>();
        builder.Services.AddSingleton<ICatImageHandler, CatImageHandler>();
        builder.Services.AddSingleton<CatImageRemover>();

        // Cats kaller alle registrerte ICatDeletedListener når en katt slettes.
        builder.Services.AddSingleton<ICatDeletedListener, DeleteImagesWhenCatIsDeleted>();

        return builder;
    }
}
