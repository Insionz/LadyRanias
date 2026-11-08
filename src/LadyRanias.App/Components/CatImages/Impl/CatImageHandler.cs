using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Posts.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Impl;

// Bildene til en katt og til et kull lagres likt: tabellraden har eierens id som PartitionKey,
// og filene ligger under «{eier}/{bilde}-thumb.webp». Id-ene er Guid-er, så en katt og et kull kan aldri kollidere.
// Det eneste som er forskjellig, er hvordan vi sjekker at eieren finnes.
internal sealed class CatImageHandler(
    ICatImageDatabase imageDatabase,
    IImageStore imageStore,
    IImageProcessor imageProcessor,
    CatImageRemover remover,
    ICatHandler catHandler,
    ILitterHandler litterHandler,
    IPostHandler postHandler,
    TimeProvider timeProvider) : ICatImageHandler
{
    internal const string ThumbnailSuffix = "thumb";
    internal const string LargeSuffix = "large";

    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxSize = 10 * 1024 * 1024;

    public Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken) =>
        UploadForOwnerAsync(catId, ct => CatExistsAsync(catId, ct), content, contentType, length, cancellationToken);

    public Task<CatImageDto?> UploadForLitterAsync(Guid litterId, Stream content, string contentType, long length, CancellationToken cancellationToken) =>
        UploadForOwnerAsync(litterId, ct => LitterExistsAsync(litterId, ct), content, contentType, length, cancellationToken);

    public Task<CatImageDto?> UploadForPostAsync(Guid postId, Stream content, string contentType, long length, CancellationToken cancellationToken) =>
        UploadForOwnerAsync(postId, ct => PostExistsAsync(postId, ct), content, contentType, length, cancellationToken);

    public Task<IReadOnlyList<CatImageDto>?> GetAllForPostAsync(Guid postId, CancellationToken cancellationToken) =>
        GetAllForOwnerAsync(postId, ct => PostExistsAsync(postId, ct), cancellationToken);

    public Task<CatImageDto?> UploadForFrontPageAsync(Stream content, string contentType, long length, CancellationToken cancellationToken) =>
        UploadForOwnerAsync(FrontPageImages.OwnerId, _ => Task.FromResult(true), content, contentType, length, cancellationToken);

    public async Task<IReadOnlyList<CatImageDto>> GetAllForFrontPageAsync(CancellationToken cancellationToken) =>
        await GetAllForOwnerAsync(FrontPageImages.OwnerId, _ => Task.FromResult(true), cancellationToken) ?? Array.Empty<CatImageDto>();

    // Et utkast regnes som funnet: mamma laster opp bilder før hun publiserer, og adminsiden henter dem herfra.
    private async Task<bool> PostExistsAsync(Guid postId, CancellationToken cancellationToken) =>
        await postHandler.GetAsync(postId, includeDrafts: true, cancellationToken) != null;

    public Task<IReadOnlyList<CatImageDto>?> GetAllAsync(Guid catId, CancellationToken cancellationToken) =>
        GetAllForOwnerAsync(catId, ct => CatExistsAsync(catId, ct), cancellationToken);

    public Task<IReadOnlyList<CatImageDto>?> GetAllForLitterAsync(Guid litterId, CancellationToken cancellationToken) =>
        GetAllForOwnerAsync(litterId, ct => LitterExistsAsync(litterId, ct), cancellationToken);

    private async Task<bool> CatExistsAsync(Guid catId, CancellationToken cancellationToken) =>
        await catHandler.GetAsync(catId, cancellationToken) != null;

    private async Task<bool> LitterExistsAsync(Guid litterId, CancellationToken cancellationToken) =>
        await litterHandler.GetAsync(litterId, cancellationToken) != null;

    private async Task<CatImageDto?> UploadForOwnerAsync(
        Guid ownerId,
        Func<CancellationToken, Task<bool>> ownerExists,
        Stream content,
        string contentType,
        long length,
        CancellationToken cancellationToken)
    {
        Validate(contentType, length);

        if (!await ownerExists(cancellationToken)) return null;

        // Kaster ValidationException hvis fila ikke er et ekte bilde.
        var processed = await imageProcessor.ProcessAsync(content, cancellationToken);

        var imageId = Guid.NewGuid();
        var thumbnailName = BlobName(ownerId, imageId, ThumbnailSuffix);
        var largeName = BlobName(ownerId, imageId, LargeSuffix);
        var now = timeProvider.GetUtcNow();

        // Datoen fra kameraet, ellers i dag. Mamma kan rette den etterpå (SetTakenAtAsync).
        var image = new CatImageEntity
        {
            CatId = ownerId,
            ImageId = imageId,
            ContentType = processed.ContentType,
            CreatedAt = now,
            TakenAt = processed.TakenAt ?? DateOnly.FromDateTime(now.UtcDateTime),
        };

        try
        {
            await SaveBytesAsync(thumbnailName, processed.Thumbnail, processed.ContentType, cancellationToken);
            await SaveBytesAsync(largeName, processed.Large, processed.ContentType, cancellationToken);

            await imageDatabase.SaveAsync(image, cancellationToken);
        }
        catch
        {
            // Kompenserende handling: fjern filene som rakk å bli lagret før feilen.
            // DeleteAsync tåler at en fil ikke finnes.
            await imageStore.DeleteAsync(thumbnailName, CancellationToken.None);
            await imageStore.DeleteAsync(largeName, CancellationToken.None);
            throw;
        }

        return ToDto(image);
    }

    private async Task<IReadOnlyList<CatImageDto>?> GetAllForOwnerAsync(
        Guid ownerId,
        Func<CancellationToken, Task<bool>> ownerExists,
        CancellationToken cancellationToken)
    {
        if (!await ownerExists(cancellationToken)) return null;

        var images = await imageDatabase.GetAllForCatAsync(ownerId, cancellationToken);

        // Sist tatt først, så kortet på kullsiden viser hvordan kattungen ser ut nå.
        // Samme dag: sist lastet opp først.
        return [.. images.Select(ToDto).OrderByDescending(image => image.TakenAt).ThenByDescending(image => image.CreatedAt)];
    }

    public async Task<CatImageFile?> OpenAsync(Guid ownerId, Guid imageId, CatImageSize size, CancellationToken cancellationToken)
    {
        // Sjekk raden først, så vi bare viser bilder som faktisk er registrert.
        var image = await imageDatabase.GetAsync(ownerId, imageId, cancellationToken);

        if (image == null) return null;

        var stored = await imageStore.OpenAsync(BlobName(ownerId, imageId, Suffix(size)), cancellationToken);

        return stored == null ? null : new CatImageFile(stored.Content, stored.ContentType);
    }

    public async Task<CatImageDto?> SetTakenAtAsync(Guid ownerId, Guid imageId, DateOnly takenAt, CancellationToken cancellationToken)
    {
        var image = await imageDatabase.GetAsync(ownerId, imageId, cancellationToken);

        if (image == null) return null;

        if (takenAt > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
        {
            throw new ValidationException("TakenAt must be today or before");
        }

        image.TakenAt = takenAt;
        await imageDatabase.SaveAsync(image, cancellationToken);

        return ToDto(image);
    }

    public Task<bool> DeleteAsync(Guid ownerId, Guid imageId, CancellationToken cancellationToken) =>
        remover.DeleteImageAsync(ownerId, imageId, cancellationToken);

    internal static string BlobName(Guid ownerId, Guid imageId, string suffix) => $"{ownerId}/{imageId}-{suffix}.webp";

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
        // Bilder fra før datoen fantes, regnes som tatt den dagen de ble lastet opp.
        TakenAt = image.TakenAt ?? DateOnly.FromDateTime(image.CreatedAt.UtcDateTime),
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
