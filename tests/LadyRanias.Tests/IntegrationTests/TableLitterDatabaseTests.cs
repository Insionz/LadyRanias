using Azure.Data.Tables;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Litters.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TableLitterDatabaseTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private TableServiceClient _serviceClient = null!;
    private string _litterTable = null!;
    private string _kittenTable = null!;
    private TableLitterDatabase _litters = null!;
    private TableKittenDatabase _kittens = null!;

    [TestInitialize]
    public void OnSetup()
    {
        // Unike tabeller per test, så testene kan kjøre samtidig.
        _litterTable = $"testlitters{Guid.NewGuid():N}";
        _kittenTable = $"testkittens{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _litters = new TableLitterDatabase(_serviceClient, _litterTable);
        _kittens = new TableKittenDatabase(_serviceClient, _kittenTable);
    }

    [TestCleanup]
    public async Task DeleteTablesAsync()
    {
        await _serviceClient.DeleteTableAsync(_litterTable);
        await _serviceClient.DeleteTableAsync(_kittenTable);
    }

    private static LitterEntity NewLitter() => new()
    {
        Id = Guid.NewGuid(),
        Name = "A-kullet",
        MotherId = Guid.NewGuid(),
        FatherId = Guid.NewGuid(),
        Status = LitterStatus.Planned,
        ExpectedDate = new DateOnly(2027, 3, 1),
        BirthDate = null,
        Description = "Test",
        CreatedAt = Start,
    };

    private static KittenEntity NewKitten(Guid litterId, string name = "Ask") => new()
    {
        Id = Guid.NewGuid(),
        LitterId = litterId,
        Name = name,
        Sex = SexEnum.Male,
        Status = KittenStatus.Available,
        Description = "Test",
        CreatedAt = Start,
    };

    [TestMethod]
    public async Task Save_then_get_returns_same_planned_litter()
    {
        var litter = NewLitter();

        await _litters.SaveAsync(litter, CancellationToken.None);

        Assert.AreEqual(litter, await _litters.GetAsync(litter.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Save_then_get_returns_same_born_litter()
    {
        // Sjekker at nullbare datoer og status går fram og tilbake riktig.
        var litter = NewLitter() with { Status = LitterStatus.Born, ExpectedDate = null, BirthDate = new DateOnly(2026, 10, 1) };

        await _litters.SaveAsync(litter, CancellationToken.None);

        Assert.AreEqual(litter, await _litters.GetAsync(litter.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAll_and_delete_litters()
    {
        var first = NewLitter();
        var second = NewLitter();
        await _litters.SaveAsync(first, CancellationToken.None);
        await _litters.SaveAsync(second, CancellationToken.None);

        Assert.HasCount(2, await _litters.GetAllAsync(CancellationToken.None));

        Assert.IsTrue(await _litters.DeleteAsync(first.Id, CancellationToken.None));
        Assert.IsFalse(await _litters.DeleteAsync(first.Id, CancellationToken.None));
        Assert.HasCount(1, await _litters.GetAllAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task Kittens_are_kept_per_litter()
    {
        var litterA = Guid.NewGuid();
        var litterB = Guid.NewGuid();
        var ask = NewKitten(litterA, "Ask");
        await _kittens.SaveAsync(ask, CancellationToken.None);
        await _kittens.SaveAsync(NewKitten(litterA, "Embla"), CancellationToken.None);
        await _kittens.SaveAsync(NewKitten(litterB, "Frøya"), CancellationToken.None);

        Assert.HasCount(2, await _kittens.GetAllForLitterAsync(litterA, CancellationToken.None));
        Assert.HasCount(1, await _kittens.GetAllForLitterAsync(litterB, CancellationToken.None));
        Assert.AreEqual(ask, await _kittens.GetAsync(litterA, ask.Id, CancellationToken.None));

        // Riktig kattunge, men feil kull: finnes ikke.
        Assert.IsNull(await _kittens.GetAsync(litterB, ask.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Save_kitten_with_new_status_overwrites_it()
    {
        var kitten = NewKitten(Guid.NewGuid());
        await _kittens.SaveAsync(kitten, CancellationToken.None);

        await _kittens.SaveAsync(kitten with { Status = KittenStatus.Reserved }, CancellationToken.None);

        var fetched = await _kittens.GetAsync(kitten.LitterId, kitten.Id, CancellationToken.None);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(KittenStatus.Reserved, fetched.Status);
    }

    [TestMethod]
    public async Task Delete_kitten()
    {
        var kitten = NewKitten(Guid.NewGuid());
        await _kittens.SaveAsync(kitten, CancellationToken.None);

        Assert.IsTrue(await _kittens.DeleteAsync(kitten.LitterId, kitten.Id, CancellationToken.None));
        Assert.IsNull(await _kittens.GetAsync(kitten.LitterId, kitten.Id, CancellationToken.None));
        Assert.IsFalse(await _kittens.DeleteAsync(kitten.LitterId, kitten.Id, CancellationToken.None));
    }
}
