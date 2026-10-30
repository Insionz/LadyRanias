using Azure.Data.Tables;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.Extensions.Time.Testing;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TableCatDatabaseTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private TableCatDatabase _database = null!;
    private TableServiceClient _serviceClient = null!;
    private string _tableName = null!;

    private static CatEntity NewCat(string name = "Bjarte") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Sex = SexEnum.Male,
        Description = "Tester",
        BirthDate = DateOnly.FromDateTime(Start.UtcDateTime),
        CreatedAt = Start,
    };

    [TestInitialize]
    public void OnSetup()
    {
        _tableName = $"testcats{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _database = new TableCatDatabase(_serviceClient, _tableName);
    }

    [TestCleanup]
    public async Task DeleteTableAsync()
    {
        await _serviceClient.DeleteTableAsync(_tableName);
    }

    [TestMethod]
    public async Task Save_then_get_returns_same_cat()
    {
        var catRequest = NewCat();

        await _database.SaveAsync(catRequest, CancellationToken.None);

        var catResponse = await _database.GetAsync(catRequest.Id, CancellationToken.None);

        Assert.AreEqual(catRequest, catResponse);
    }

    [TestMethod]
    public async Task Save_existing_cat_overwrites_it()
    {
        var catRequest = NewCat();

        await _database.SaveAsync(catRequest, CancellationToken.None);
        var cat = await _database.GetAsync(catRequest.Id, CancellationToken.None);
        Assert.IsNotNull(cat);
        Assert.AreEqual(catRequest.Name, cat.Name);
        Assert.AreEqual(catRequest, cat);

        await _database.SaveAsync(catRequest with { Name = "Bjørn" },  CancellationToken.None);
        cat = await _database.GetAsync(catRequest.Id, CancellationToken.None);
        Assert.IsNotNull(cat);
        Assert.AreEqual("Bjørn", cat.Name);
    }

    [TestMethod]
    public async Task GetAll_returns_all_saved_cats() 
    {
        var catRequest = NewCat();

        var all = await _database.GetAllAsync(CancellationToken.None);

        Assert.HasCount(0, all);

        await _database.SaveAsync(catRequest, CancellationToken.None);
        all = await _database.GetAllAsync(CancellationToken.None);

        Assert.HasCount(1, all);

        await _database.SaveAsync(catRequest with { Id = Guid.NewGuid() }, CancellationToken.None);
        all = await _database.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, all);
    }

    [TestMethod]
    public async Task Delete_existing_cat_returns_true_and_removes_it()
    {
        var catRequest = NewCat();

        await _database.SaveAsync(catRequest, CancellationToken.None);

        var deleted = await _database.DeleteAsync(catRequest.Id, CancellationToken.None);

        Assert.IsTrue(deleted);

        var get = await _database.GetAsync(catRequest.Id, CancellationToken.None);

        Assert.IsNull(get);
    }

    [TestMethod]
    public async Task Delete_unknown_id_returns_false()
    {
        var deleted = await _database.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsFalse(deleted);
    }
}
