using Azure.Data.Tables;
using Azure.Storage.Blobs;
using LadyRanias.App.Components.CatImages.Impl;
using LadyRanias.App.Components.Cats.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class BlobImageStoreTests
{
    private BlobImageStore _database = null!;
    private BlobServiceClient _serviceClient = null!;
    private string _blobContainerName = null!;

    [TestInitialize]
    public void OnSetup()
    {
        _blobContainerName = $"test-cat-container{Guid.NewGuid():N}";
        _serviceClient = new BlobServiceClient("UseDevelopmentStorage=true");
        _database = new BlobImageStore(_serviceClient, _blobContainerName);
    }

    [TestCleanup]
    public async Task OnCleanup()
    {
        await _serviceClient.DeleteBlobContainerAsync(_blobContainerName);
    }

    [TestMethod]
    public async Task Save_then_open_returns_same_bytes_and_content_type()
    {
        var name = "test.webp";

        byte[] original = "hei"u8.ToArray();
        using var upload = new MemoryStream(original);


        await _database.SaveAsync(name, upload, "image/webp", CancellationToken.None);
        var response = await _database.OpenAsync(name, CancellationToken.None);

        Assert.IsNotNull(response);
        Assert.AreEqual("image/webp", response.ContentType);

        using var downloaded = new MemoryStream();
        await response.Content.CopyToAsync(downloaded);
        CollectionAssert.AreEqual(original, downloaded.ToArray());
    }

    [TestMethod]
    public async Task Open_unknown_name_returns_null()
    {
        var response = await _database.OpenAsync("test.webp", CancellationToken.None);
        Assert.IsNull(response);
    }

    [TestMethod]
    public async Task Delete_existing_returns_true_and_open_returns_null()
    {
        var name = "test.webp";

        byte[] original = "hei"u8.ToArray();
        using var upload = new MemoryStream(original);

        await _database.SaveAsync(name, upload, "image/webp", CancellationToken.None);

        var deleted = await _database.DeleteAsync(name, CancellationToken.None);
        Assert.IsTrue(deleted);

        var response = await _database.OpenAsync(name, CancellationToken.None);
        Assert.IsNull(response);
    }

    [TestMethod]
    public async Task Delete_unknown_returns_false()
    {
        var deleted = await _database.DeleteAsync("test.webp", CancellationToken.None);
        Assert.IsFalse(deleted);
    }
}
