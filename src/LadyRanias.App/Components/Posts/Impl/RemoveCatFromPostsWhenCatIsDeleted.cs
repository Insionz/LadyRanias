using LadyRanias.App.Components.Cats.Api;

namespace LadyRanias.App.Components.Posts.Impl;

/// <summary>Lytter på slettede katter, og fjerner dem fra innleggene. Innleggene blir stående.</summary>
/// <remarks>
/// Bruker databasen direkte, ikke PostHandler: PostHandler trenger ICatHandler, og CatHandler trenger lytterne.
/// Da ville DI fått en sirkel. Samme grunn som CatImageRemover i CatImages.
/// </remarks>
internal sealed class RemoveCatFromPostsWhenCatIsDeleted(IPostDatabase database) : ICatDeletedListener
{
    public async Task OnCatDeletedAsync(Guid catId, CancellationToken cancellationToken)
    {
        foreach (var post in await database.GetAllAsync(cancellationToken))
        {
            if (post.CatIds.Contains(catId))
            {
                await database.SaveAsync(post with { CatIds = [.. post.CatIds.Where(id => id != catId)] }, cancellationToken);
            }
        }
    }
}
