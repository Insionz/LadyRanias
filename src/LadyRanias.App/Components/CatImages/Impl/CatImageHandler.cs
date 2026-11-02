using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.Cats.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class CatImageHandler(ICatImageDatabase imageDatabase, IImageStore imageStore, ICatHandler catHandler, TimeProvider timeProvider) : ICatImageHandler
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxSize = 10 * 1024 * 1024;

    public async Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken)
    {
        Validate(contentType, length);

        var cat = await catHandler.GetAsync(catId, cancellationToken);

        if (cat == null) return null;

        var imageId = Guid.NewGuid();
        var blobName = $"{cat.Id}/{imageId}";

        await imageStore.SaveAsync(blobName, content, contentType, cancellationToken);

        var now = timeProvider.GetUtcNow();

        try
        {
            await imageDatabase.SaveAsync(new CatImageEntity
            {
                CatId = catId,
                ContentType = contentType,
                ImageId = imageId,
                CreatedAt = now
            }, cancellationToken);
        }
        catch (Exception)
        {
            await imageStore.DeleteAsync(blobName, CancellationToken.None);
            throw;
        }

        return new CatImageDto
        {
            ImageId = imageId,
            CreatedAt = now
        };
    }

    private static void Validate(string contentType, long length)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new ValidationException("ContentType must be image/jpeg, image/png or image/webp.");

        if (length > MaxSize)
            throw new ValidationException("File must not exceed 10 MB.");
    }
}
