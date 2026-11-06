using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>Lytter på slettede kull fra Litters-komponenten, og fjerner bildene av kullet.</summary>
/// <remarks>Bildene ligger under kullets id, akkurat som en katts bilder ligger under kattens id.</remarks>
internal sealed class DeleteImagesWhenLitterIsDeleted(CatImageRemover remover) : ILitterDeletedListener
{
    public Task OnLitterDeletedAsync(Guid litterId, CancellationToken cancellationToken) =>
        remover.DeleteAllForCatAsync(litterId, cancellationToken);
}
