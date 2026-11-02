namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>
/// Sletter bilder: både raden i tabellen og begge filene.
/// Brukes av CatImageHandler (slett ett bilde) og DeleteImagesWhenCatIsDeleted (slett alle bildene til en katt).
/// </summary>
/// <remarks>
/// Hvorfor en egen klasse, og ikke bare kalle CatImageHandler fra lytteren?
/// CatImageHandler trenger ICatHandler, og CatHandler trenger lytterne. Hvis lytteren trengte CatImageHandler,
/// ville DI fått en sirkel (CatHandler → lytter → CatImageHandler → CatHandler) og kastet en exception ved oppstart.
/// </remarks>
internal sealed class CatImageRemover(ICatImageDatabase imageDatabase, IImageStore imageStore)
{
    public async Task<bool> DeleteImageAsync(Guid catId, Guid imageId, CancellationToken cancellationToken)
    {
        // Raden først: da forsvinner bildet fra siden med en gang.
        // Feiler slettingen av filene etterpå, blir de liggende uten at noen ser dem.
        var deleted = await imageDatabase.DeleteAsync(catId, imageId, cancellationToken);

        if (!deleted)
        {
            return false;
        }

        await imageStore.DeleteAsync(CatImageHandler.BlobName(catId, imageId, CatImageHandler.ThumbnailSuffix), CancellationToken.None);
        await imageStore.DeleteAsync(CatImageHandler.BlobName(catId, imageId, CatImageHandler.LargeSuffix), CancellationToken.None);

        return true;
    }

    public async Task DeleteAllForCatAsync(Guid catId, CancellationToken cancellationToken)
    {
        var images = await imageDatabase.GetAllForCatAsync(catId, cancellationToken);

        foreach (var image in images)
        {
            await DeleteImageAsync(catId, image.ImageId, cancellationToken);
        }
    }
}
