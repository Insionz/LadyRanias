using System.ComponentModel.DataAnnotations;
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

    // De fire første bytene i en PNG-fil. Nok til å teste lagring, uten å være et ekte bilde.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47];

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryCatImageDatabase _imageDatabase = new();
    private readonly InMemoryImageStore _imageStore = new();
    private readonly CatHandler _catHandler;
    private readonly CatImageHandler _handler;
    private CatDto _kiara = null!;

    public CatImageHandlerTests()
    {
        // Ekte CatHandler med database i minnet, slik at "finnes katten?" testes gjennom Cats-komponentens API.
        _catHandler = new CatHandler(new InMemoryCatDatabase(), _clock);
        _handler = new CatImageHandler(_imageDatabase, _imageStore, _catHandler, _clock);
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
    public async Task Upload_valid_png_saves_file_and_row()
    {
        using var content = new MemoryStream(PngBytes);

        var result = await _handler.UploadAsync(_kiara.Id, content, "image/png", PngBytes.Length, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(Start, result.CreatedAt);

        var row = await _imageDatabase.GetAsync(_kiara.Id, result.ImageId, CancellationToken.None);
        Assert.IsNotNull(row);
        Assert.AreEqual("image/png", row.ContentType);

        var file = await _imageStore.OpenAsync($"{_kiara.Id}/{result.ImageId}", CancellationToken.None);
        Assert.IsNotNull(file);
        using var saved = new MemoryStream();
        await file.Content.CopyToAsync(saved);
        CollectionAssert.AreEqual(PngBytes, saved.ToArray());
    }

    [TestMethod]
    public async Task Upload_for_unknown_cat_returns_null_and_saves_nothing()
    {
        var unknownCatId = Guid.NewGuid();
        using var content = new MemoryStream(PngBytes);

        var result = await _handler.UploadAsync(unknownCatId, content, "image/png", PngBytes.Length, CancellationToken.None);

        Assert.IsNull(result);
        Assert.HasCount(0, await _imageDatabase.GetAllForCatAsync(unknownCatId, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("image/gif")]
    [DataRow("application/pdf")]
    [DataRow("")]
    public async Task Upload_with_unsupported_content_type_is_rejected(string contentType)
    {
        using var content = new MemoryStream(PngBytes);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UploadAsync(_kiara.Id, content, contentType, PngBytes.Length, CancellationToken.None));
    }

    [TestMethod]
    public async Task Upload_larger_than_10_MB_is_rejected()
    {
        // Strømmen er liten, men length sier at fila er for stor. Handleren skal stole på length.
        using var content = new MemoryStream(PngBytes);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UploadAsync(_kiara.Id, content, "image/png", TenMegabytes + 1, CancellationToken.None));
    }

    [TestMethod]
    public async Task Upload_of_exactly_10_MB_is_accepted()
    {
        using var content = new MemoryStream(PngBytes);

        var result = await _handler.UploadAsync(_kiara.Id, content, "image/png", TenMegabytes, CancellationToken.None);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Upload_removes_file_when_saving_row_fails()
    {
        var store = new TrackingImageStore();
        var handler = new CatImageHandler(new FailingCatImageDatabase(), store, _catHandler, _clock);
        using var content = new MemoryStream(PngBytes);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.UploadAsync(_kiara.Id, content, "image/png", PngBytes.Length, CancellationToken.None));

        Assert.HasCount(1, store.Saved);
        CollectionAssert.AreEqual(store.Saved, store.Deleted);
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
