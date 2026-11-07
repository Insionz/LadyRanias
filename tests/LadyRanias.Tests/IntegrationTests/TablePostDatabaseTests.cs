using Azure.Data.Tables;
using LadyRanias.App.Components.Posts.Api;
using LadyRanias.App.Components.Posts.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TablePostDatabaseTests
{
    private TableServiceClient _serviceClient = null!;
    private string _tableName = null!;
    private TablePostDatabase _database = null!;

    [TestInitialize]
    public void OnSetup()
    {
        _tableName = $"testposts{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _database = new TablePostDatabase(_serviceClient, _tableName);
    }

    [TestCleanup]
    public async Task DeleteTableAsync()
    {
        await _serviceClient.DeleteTableAsync(_tableName);
    }

    private static PostEntity NewPost(params Guid[] catIds) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Utstilling i Hamar",
        Body = "Første avsnitt.\n\nAndre avsnitt med æøå.",
        Date = new DateOnly(2026, 10, 3),
        Kind = PostKind.Show,
        CatIds = catIds,
        Published = true,
        CreatedAt = new DateTimeOffset(2026, 10, 4, 8, 0, 0, TimeSpan.Zero),
    };

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public async Task Save_then_get_returns_same_post(int cats)
    {
        // Kattene lagres som tekst med komma. Sjekker ingen, én og flere.
        var post = NewPost([.. Enumerable.Range(0, cats).Select(_ => Guid.NewGuid())]);

        await _database.SaveAsync(post, CancellationToken.None);

        Assert.AreEqual(post, await _database.GetAsync(post.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAll_and_delete()
    {
        var post = NewPost();
        await _database.SaveAsync(post, CancellationToken.None);
        await _database.SaveAsync(NewPost(), CancellationToken.None);

        Assert.HasCount(2, await _database.GetAllAsync(CancellationToken.None));
        Assert.IsTrue(await _database.DeleteAsync(post.Id, CancellationToken.None));
        Assert.IsFalse(await _database.DeleteAsync(post.Id, CancellationToken.None));
        Assert.HasCount(1, await _database.GetAllAsync(CancellationToken.None));
    }
}
