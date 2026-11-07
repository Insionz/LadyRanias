using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.CatImages.Impl;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Posts.Api;
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

        // Kull kan også ha bilder (hele kullet sammen). Litters sier fra når et kull slettes.
        builder.Services.AddSingleton<ILitterDeletedListener, DeleteImagesWhenLitterIsDeleted>();

        // Og innlegg i nyhetene.
        builder.Services.AddSingleton<IPostDeletedListener, DeleteImagesWhenPostIsDeleted>();

        return builder;
    }
}
