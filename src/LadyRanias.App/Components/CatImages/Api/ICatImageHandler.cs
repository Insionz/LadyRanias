namespace LadyRanias.App.Components.CatImages.Api;

public interface ICatImageHandler
{
    /// <returns><c>null</c> hvis katten ikke finnes.</returns>
    Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken);

    /// <returns>Bildene, nyeste først, eller <c>null</c> hvis katten ikke finnes.</returns>
    Task<IReadOnlyList<CatImageDto>?> GetAllAsync(Guid catId, CancellationToken cancellationToken);

    /// <returns><c>null</c> hvis bildet ikke finnes.</returns>
    Task<CatImageFile?> OpenAsync(Guid catId, Guid imageId, CatImageSize size, CancellationToken cancellationToken);

    /// <returns><c>false</c> hvis bildet ikke finnes.</returns>
    Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken);
}
