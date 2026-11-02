using LadyRanias.App.Components.Cats.Api;

namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>Lytter på slettede katter fra Cats-komponenten, og fjerner bildene deres.</summary>
internal sealed class DeleteImagesWhenCatIsDeleted(CatImageRemover remover) : ICatDeletedListener
{
    public Task OnCatDeletedAsync(Guid catId, CancellationToken cancellationToken) =>
        remover.DeleteAllForCatAsync(catId, cancellationToken);
}
