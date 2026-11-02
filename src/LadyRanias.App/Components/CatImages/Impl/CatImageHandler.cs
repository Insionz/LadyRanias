using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.Cats.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class CatImageHandler(
    ICatImageDatabase imageDatabase,
    IImageStore imageStore,
    IImageProcessor imageProcessor,
    CatImageRemover remover,
    ICatHandler catHandler,
    TimeProvider timeProvider) : ICatImageHandler
{
    internal const string ThumbnailSuffix = "thumb";
    internal const string LargeSuffix = "large";

    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxSize = 10 * 1024 * 1024;

    public async Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken)
    {
        Validate(contentType, length);

        var cat = await catHandler.GetAsync(catId, cancellationToken);

        if (cat == null) return null;

        // Kaster ValidationException hvis fila ikke er et ekte bilde.
        var processed = await imageProcessor.ProcessAsync(content, cancellationToken);

        var imageId = Guid.NewGuid();
        var thumbnailName = BlobName(catId, imageId, ThumbnailSuffix);
        var largeName = BlobName(catId, imageId, LargeSuffix);
        var now = timeProvider.GetUtcNow();

        try
        {
            await SaveBytesAsync(thumbnailName, processed.Thumbnail, processed.ContentType, cancellationToken);
            await SaveBytesAsync(largeName, processed.Large, processed.ContentType, cancellationToken);

            await imageDatabase.SaveAsync(new CatImageEntity
            {
                CatId = catId,
                ImageId = imageId,
                ContentType = processed.ContentType,
                CreatedAt = now,
            }, cancellationToken);
        }
        catch
        {
            // Kompenserende handling: fjern filene som rakk å bli lagret før feilen.
            // DeleteAsync tåler at en fil ikke finnes.
            await imageStore.DeleteAsync(thumbnailName, CancellationToken.None);
            await imageStore.DeleteAsync(largeName, CancellationToken.None);
            throw;
        }

        return new CatImageDto
        {
            ImageId = imageId,
            CreatedAt = now,
        };
    }

    public async Task<IReadOnlyList<CatImageDto>?> GetAllAsync(Guid catId, CancellationToken cancellationToken)
    {
        var cat = await catHandler.GetAsync(catId, cancellationToken);

        if (cat == null) return null;

        var images = await imageDatabase.GetAllForCatAsync(catId, cancellationToken);

        return [.. images.OrderByDescending(image => image.CreatedAt).Select(ToDto)];
    }

    public async Task<CatImageFile?> OpenAsync(Guid catId, Guid imageId, CatImageSize size, CancellationToken cancellationToken)
    {
        // Sjekk raden først, så vi bare viser bilder som faktisk er registrert.
        var image = await imageDatabase.GetAsync(catId, imageId, cancellationToken);

        if (image == null) return null;

        var stored = await imageStore.OpenAsync(BlobName(catId, imageId, Suffix(size)), cancellationToken);

        return stored == null ? null : new CatImageFile(stored.Content, stored.ContentType);
    }

    public Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken) =>
        remover.DeleteImageAsync(catId, imageId, cancellationToken);

    internal static string BlobName(Guid catId, Guid imageId, string suffix) => $"{catId}/{imageId}-{suffix}.webp";

    private static string Suffix(CatImageSize size) => size switch
    {
        CatImageSize.Thumb => ThumbnailSuffix,
        CatImageSize.Large => LargeSuffix,
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };

    private static CatImageDto ToDto(CatImageEntity image) => new()
    {
        ImageId = image.ImageId,
        CreatedAt = image.CreatedAt,
    };

    private async Task SaveBytesAsync(string name, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(bytes);
        await imageStore.SaveAsync(name, stream, contentType, cancellationToken);
    }

    private static void Validate(string contentType, long length)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new ValidationException("ContentType must be image/jpeg, image/png or image/webp.");

        if (length > MaxSize)
            throw new ValidationException("File must not exceed 10 MB.");
    }
}
