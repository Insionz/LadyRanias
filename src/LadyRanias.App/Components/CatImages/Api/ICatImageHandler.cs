namespace LadyRanias.App.Components.CatImages.Api;

/// <summary>
/// Bilder av en katt eller av et helt kull. Begge har en Guid, og bildene lagres likt («eieren» er katten eller kullet).
/// Open, SetTakenAt og Delete tar derfor bare eierens id. Opplasting og liste sjekker at eieren finnes, så de finnes i to utgaver.
/// </summary>
public interface ICatImageHandler
{
    /// <returns><c>null</c> hvis katten ikke finnes.</returns>
    Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken);

    /// <returns><c>null</c> hvis kullet ikke finnes.</returns>
    Task<CatImageDto?> UploadForLitterAsync(Guid litterId, Stream content, string contentType, long length, CancellationToken cancellationToken);

    /// <returns><c>null</c> hvis innlegget ikke finnes (også et utkast regnes som funnet).</returns>
    Task<CatImageDto?> UploadForPostAsync(Guid postId, Stream content, string contentType, long length, CancellationToken cancellationToken);

    /// <returns>Bildene, sist tatt først, eller <c>null</c> hvis katten ikke finnes.</returns>
    Task<IReadOnlyList<CatImageDto>?> GetAllAsync(Guid catId, CancellationToken cancellationToken);

    /// <returns>Bildene, sist tatt først, eller <c>null</c> hvis kullet ikke finnes.</returns>
    Task<IReadOnlyList<CatImageDto>?> GetAllForLitterAsync(Guid litterId, CancellationToken cancellationToken);

    /// <returns>Bildene til et innlegg, eller <c>null</c> hvis innlegget ikke finnes.</returns>
    Task<IReadOnlyList<CatImageDto>?> GetAllForPostAsync(Guid postId, CancellationToken cancellationToken);

    /// <param name="ownerId">Katten, kullet eller innlegget.</param>
    /// <returns><c>null</c> hvis bildet ikke finnes.</returns>
    Task<CatImageFile?> OpenAsync(Guid ownerId, Guid imageId, CatImageSize size, CancellationToken cancellationToken);

    /// <summary>Retter datoen bildet ble tatt. ValidationException hvis den er i framtiden.</summary>
    /// <returns><c>null</c> hvis bildet ikke finnes.</returns>
    Task<CatImageDto?> SetTakenAtAsync(Guid ownerId, Guid imageId, DateOnly takenAt, CancellationToken cancellationToken);

    /// <returns><c>false</c> hvis bildet ikke finnes.</returns>
    Task<bool> DeleteAsync(Guid ownerId, Guid imageId, CancellationToken cancellationToken);
}
