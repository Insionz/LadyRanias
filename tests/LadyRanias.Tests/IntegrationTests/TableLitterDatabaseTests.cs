using Azure.Data.Tables;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Litters.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TableLitterDatabaseTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private TableServiceClient _serviceClient = null!;
    private string _litterTable = null!;
    private TableLitterDatabase _litters = null!;

    [TestInitialize]
    public void OnSetup()
    {
        // Unike tabeller per test, så testene kan kjøre samtidig.
        _litterTable = $"testlitters{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _litters = new TableLitterDatabase(_serviceClient, _litterTable);
    }

    [TestCleanup]
    public async Task DeleteTablesAsync()
    {
        await _serviceClient.DeleteTableAsync(_litterTable);
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
}
