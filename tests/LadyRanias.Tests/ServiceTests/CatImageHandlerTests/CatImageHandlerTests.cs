using System.ComponentModel.DataAnnotations;
using LadyRanias.App.Components.CatImages.Api;
using LadyRanias.App.Components.CatImages.Impl;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.Extensions.Time.Testing;

namespace LadyRanias.Tests.ServiceTests.CatImageHandlerTests;

[TestClass]
public sealed class CatImageHandlerTests
{
    private const long TenMegabytes = 10 * 1024 * 1024;
    private static readonly DateTimeOffset Start = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    // Handleren sender innholdet rett til IImageProcessor, så det trenger ikke være et ekte bilde her.
    // Ekte bildebehandling testes i SkiaSharpImageProcessorTests.
    private static readonly byte[] UploadBytes = [1, 2, 3, 4];

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryCatImageDatabase _imageDatabase = new();
    private readonly InMemoryImageStore _imageStore = new();
    private readonly FakeImageProcessor _processor = new();
    private readonly CatImageRemover _remover;
    private readonly CatHandler _catHandler;
    private readonly CatImageHandler _handler;
    private CatDto _kiara = null!;

    public CatImageHandlerTests()
    {
        _remover = new CatImageRemover(_imageDatabase, _imageStore);

        // Ekte CatHandler med database i minnet, slik at "finnes katten?" testes gjennom Cats-komponentens API.
        // Lytteren er med, så sletting av en katt også rydder bildene, slik det skjer i appen.
        _catHandler = new CatHandler(new InMemoryCatDatabase(), _clock, [new DeleteImagesWhenCatIsDeleted(_remover)]);
        _handler = new CatImageHandler(_imageDatabase, _imageStore, _processor, _remover, _catHandler, _clock);
    }

    [TestInitialize]
    public async Task CreateCatAsync()
    {
        _kiara = await _catHandler.CreateAsync(new CreateCatDto
        {
            Name = "Kiara",
            Sex = SexEnum.Female,
            BirthDate = new DateOnly(2024, 3, 1),
            Description = "Testkatt",
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Upload_valid_image_saves_both_sizes_and_row()
    {
        using var content = new MemoryStream(UploadBytes);

        var result = await _handler.UploadAsync(_kiara.Id, content, "image/png", UploadBytes.Length, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(Start, result.CreatedAt);

        var row = await _imageDatabase.GetAsync(_kiara.Id, result.ImageId, CancellationToken.None);
        Assert.IsNotNull(row);
        Assert.AreEqual("image/webp", row.ContentType);

        CollectionAssert.AreEqual(FakeImageProcessor.Thumbnail,
            await ReadAsync(CatImageHandler.BlobName(_kiara.Id, result.ImageId, CatImageHandler.ThumbnailSuffix)));
        CollectionAssert.AreEqual(FakeImageProcessor.Large,
            await ReadAsync(CatImageHandler.BlobName(_kiara.Id, result.ImageId, CatImageHandler.LargeSuffix)));
    }

    [TestMethod]
    public async Task Upload_for_unknown_cat_returns_null_and_saves_nothing()
    {
        var unknownCatId = Guid.NewGuid();
        using var content = new MemoryStream(UploadBytes);

        var result = await _handler.UploadAsync(unknownCatId, content, "image/png", UploadBytes.Length, CancellationToken.None);

        Assert.IsNull(result);
        Assert.HasCount(0, await _imageDatabase.GetAllForCatAsync(unknownCatId, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("image/gif")]
    [DataRow("application/pdf")]
    [DataRow("")]
    public async Task Upload_with_unsupported_content_type_is_rejected(string contentType)
    {
        using var content = new MemoryStream(UploadBytes);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UploadAsync(_kiara.Id, content, contentType, UploadBytes.Length, CancellationToken.None));
    }

    [TestMethod]
    public async Task Upload_larger_than_10_MB_is_rejected()
    {
        // Strømmen er liten, men length sier at fila er for stor. Handleren skal stole på length.
        using var content = new MemoryStream(UploadBytes);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UploadAsync(_kiara.Id, content, "image/png", TenMegabytes + 1, CancellationToken.None));
    }

    [TestMethod]
    public async Task Upload_of_exactly_10_MB_is_accepted()
    {
        using var content = new MemoryStream(UploadBytes);

        var result = await _handler.UploadAsync(_kiara.Id, content, "image/png", TenMegabytes, CancellationToken.None);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Upload_removes_both_files_when_saving_row_fails()
    {
        var store = new TrackingImageStore();
        var failingDatabase = new FailingCatImageDatabase();
        var handler = new CatImageHandler(failingDatabase, store, _processor, new CatImageRemover(failingDatabase, store), _catHandler, _clock);
        using var content = new MemoryStream(UploadBytes);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.UploadAsync(_kiara.Id, content, "image/png", UploadBytes.Length, CancellationToken.None));

        Assert.HasCount(2, store.Saved);
        CollectionAssert.AreEquivalent(store.Saved, store.Deleted);
    }

    [TestMethod]
    public async Task GetAll_returns_images_newest_first()
    {
        var first = await UploadAsync();
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = await UploadAsync();

        var images = await _handler.GetAllAsync(_kiara.Id, CancellationToken.None);

        Assert.IsNotNull(images);
        Assert.HasCount(2, images);
        Assert.AreEqual(second.ImageId, images[0].ImageId);
        Assert.AreEqual(first.ImageId, images[1].ImageId);
    }

    [TestMethod]
    public async Task GetAll_for_unknown_cat_returns_null()
    {
        var images = await _handler.GetAllAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(images);
    }

    [TestMethod]
    [DataRow(CatImageSize.Thumb)]
    [DataRow(CatImageSize.Large)]
    public async Task Open_returns_requested_size(CatImageSize size)
    {
        var uploaded = await UploadAsync();

        var file = await _handler.OpenAsync(_kiara.Id, uploaded.ImageId, size, CancellationToken.None);

        Assert.IsNotNull(file);
        Assert.AreEqual("image/webp", file.ContentType);
        var expected = size == CatImageSize.Thumb ? FakeImageProcessor.Thumbnail : FakeImageProcessor.Large;
        using var copy = new MemoryStream();
        await file.Content.CopyToAsync(copy);
        CollectionAssert.AreEqual(expected, copy.ToArray());
    }

    [TestMethod]
    public async Task Open_unknown_image_returns_null()
    {
        var file = await _handler.OpenAsync(_kiara.Id, Guid.NewGuid(), CatImageSize.Large, CancellationToken.None);

        Assert.IsNull(file);
    }

    [TestMethod]
    public async Task Delete_removes_row_and_both_files()
    {
        var uploaded = await UploadAsync();

        var deleted = await _handler.DeleteAsync(_kiara.Id, uploaded.ImageId, CancellationToken.None);

        Assert.IsTrue(deleted);
        Assert.IsNull(await _imageDatabase.GetAsync(_kiara.Id, uploaded.ImageId, CancellationToken.None));
        await AssertNoFilesAsync(uploaded.ImageId);
    }

    [TestMethod]
    public async Task Delete_unknown_image_returns_false()
    {
        var deleted = await _handler.DeleteAsync(_kiara.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public async Task Deleting_cat_removes_all_its_images()
    {
        var first = await UploadAsync();
        var second = await UploadAsync();

        await _catHandler.DeleteAsync(_kiara.Id, CancellationToken.None);

        Assert.HasCount(0, await _imageDatabase.GetAllForCatAsync(_kiara.Id, CancellationToken.None));
        await AssertNoFilesAsync(first.ImageId);
        await AssertNoFilesAsync(second.ImageId);
    }

    private async Task<CatImageDto> UploadAsync()
    {
        using var content = new MemoryStream(UploadBytes);
        var result = await _handler.UploadAsync(_kiara.Id, content, "image/png", UploadBytes.Length, CancellationToken.None);
        Assert.IsNotNull(result);
        return result;
    }

    private async Task AssertNoFilesAsync(Guid imageId)
    {
        Assert.IsNull(await _imageStore.OpenAsync(CatImageHandler.BlobName(_kiara.Id, imageId, CatImageHandler.ThumbnailSuffix), CancellationToken.None));
        Assert.IsNull(await _imageStore.OpenAsync(CatImageHandler.BlobName(_kiara.Id, imageId, CatImageHandler.LargeSuffix), CancellationToken.None));
    }

    private async Task<byte[]> ReadAsync(string blobName)
    {
        var stored = await _imageStore.OpenAsync(blobName, CancellationToken.None);
        Assert.IsNotNull(stored, $"Fant ikke {blobName}");
        using var copy = new MemoryStream();
        await stored.Content.CopyToAsync(copy);
        return copy.ToArray();
    }

    /// <summary>Gir faste bytes tilbake, så handleren kan testes uten ekte bildebehandling.</summary>
    private sealed class FakeImageProcessor : IImageProcessor
    {
        public static readonly byte[] Thumbnail = [10, 11];
        public static readonly byte[] Large = [20, 21, 22];

        public Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken) =>
            Task.FromResult(new ProcessedImage(Thumbnail, Large, "image/webp"));
    }

    /// <summary>Husker hvilke filer som ble lagret og slettet, så testen kan sjekke oppryddingen.</summary>
    private sealed class TrackingImageStore : IImageStore
    {
        private readonly InMemoryImageStore _inner = new();

        public List<string> Saved { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken)
        {
            Saved.Add(name);
            return _inner.SaveAsync(name, content, contentType, cancellationToken);
        }

        public Task<StoredImage?> OpenAsync(string name, CancellationToken cancellationToken) =>
            _inner.OpenAsync(name, cancellationToken);

        public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
        {
            Deleted.Add(name);
            return _inner.DeleteAsync(name, cancellationToken);
        }
    }

    /// <summary>En database som alltid feiler ved lagring, for å teste kompenserende handling.</summary>
    private sealed class FailingCatImageDatabase : ICatImageDatabase
    {
        public Task SaveAsync(CatImageEntity entity, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulert feil i Table Storage");

        public Task<IReadOnlyList<CatImageEntity>> GetAllForCatAsync(Guid catId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CatImageEntity?> GetAsync(Guid catId, Guid imageId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
