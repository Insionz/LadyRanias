using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Posts.Api;
using LadyRanias.App.Components.Posts.Impl;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LadyRanias.App.Components.Posts;

public static class PostsComponent
{
    /// <summary>Krever at UseCatsComponent() også er kalt, fordi Posts bruker ICatHandler.</summary>
    public static WebApplicationBuilder UsePostsComponent(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IPostDatabase, TablePostDatabase>();
        builder.Services.AddSingleton<IPostHandler, PostHandler>();

        // Cats sier fra når en katt slettes, så den kan fjernes fra innleggene.
        builder.Services.AddSingleton<ICatDeletedListener, RemoveCatFromPostsWhenCatIsDeleted>();

        return builder;
    }
}
