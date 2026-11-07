using LadyRanias.App.Components.Posts.Api;

namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>Lytter på slettede innlegg fra Posts-komponenten, og fjerner bildene deres.</summary>
internal sealed class DeleteImagesWhenPostIsDeleted(CatImageRemover remover) : IPostDeletedListener
{
    public Task OnPostDeletedAsync(Guid postId, CancellationToken cancellationToken) =>
        remover.DeleteAllForCatAsync(postId, cancellationToken);
}
