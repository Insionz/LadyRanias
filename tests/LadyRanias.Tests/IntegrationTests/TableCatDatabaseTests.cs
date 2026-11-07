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
    public async Task Save_then_get_returns_same_kitten()
    {
        // LitterId og SaleStatus går fram og tilbake. Katter uten dem (de vanlige) testes over.
        var kitten = NewCat() with { LitterId = Guid.NewGuid(), SaleStatus = KittenStatus.Reserved };

        await _database.SaveAsync(kitten, CancellationToken.None);

        Assert.AreEqual(kitten, await _database.GetAsync(kitten.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Save_then_get_returns_all_new_fields()
    {
        var cat = NewCat() with
        {
            Breed = Breed.MaineCoon,
            Role = CatRole.Retired,
            EmsCode = "MCO n 22",
            Color = "Svart tabby",
            Neutered = true,
            RegistrationNumber = "NO*LadyRanias Bjarte",
            Titles = "IC",
            Deceased = true,
            DeathDate = new DateOnly(2026, 2, 3),
            MotherId = Guid.NewGuid(),
            FatherName = "IC NO*Skogheim's Balder",
        };

        await _database.SaveAsync(cat, CancellationToken.None);

        Assert.AreEqual(cat, await _database.GetAsync(cat.Id, CancellationToken.None));
    }

    [TestMethod]
    [DataRow(false, CatRole.Breeding)]
    [DataRow(true, CatRole.Kitten)]
    public async Task Cat_saved_before_the_new_fields_gets_defaults(bool bornHere, CatRole expectedRole)
    {
        // Slik ser en rad ut som ble lagret før steg 14 del 2: ingen Breed, Role, EmsCode og så videre.
        var id = Guid.NewGuid();
        await _serviceClient.GetTableClient(_tableName).UpsertEntityAsync(new CatTableEntity
        {
            PartitionKey = "cat",
            RowKey = id.ToString(),
            Name = "Gammel",
            Sex = "Female",
            BirthDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTimeOffset.UtcNow,
            LitterId = bornHere ? Guid.NewGuid() : null,
        });

        var cat = await _database.GetAsync(id, CancellationToken.None);

        Assert.IsNotNull(cat);
        Assert.AreEqual(Breed.Unknown, cat.Breed);
        Assert.AreEqual(expectedRole, cat.Role);
        Assert.IsFalse(cat.Neutered);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Cat_saved_with_only_death_date_is_deceased(bool hasDeathDate)
    {
        // Slik ser en rad ut fra før «død uten dato» fantes: bare DeathDate, ingen Deceased-kolonne.
        var id = Guid.NewGuid();
        await _serviceClient.GetTableClient(_tableName).UpsertEntityAsync(new CatTableEntity
        {
            PartitionKey = "cat",
            RowKey = id.ToString(),
            Name = "Gammel",
            Sex = "Female",
            BirthDate = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTimeOffset.UtcNow,
            DeathDate = hasDeathDate ? new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) : null,
        });

        var cat = await _database.GetAsync(id, CancellationToken.None);

        Assert.IsNotNull(cat);
        Assert.AreEqual(hasDeathDate, cat.Deceased);
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
