using Azure.Data.Tables;
using LadyRanias.App.Components.Site.Api;
using LadyRanias.App.Components.Site.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TableSiteDatabaseTests
{
    private TableServiceClient _serviceClient = null!;
    private string _tableName = null!;
    private TableSiteDatabase _database = null!;

    [TestInitialize]
    public void OnSetup()
    {
        _tableName = $"testsite{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _database = new TableSiteDatabase(_serviceClient, _tableName);
    }

    [TestCleanup]
    public async Task DeleteTableAsync()
    {
        await _serviceClient.DeleteTableAsync(_tableName);
    }

    [TestMethod]
    public async Task Nothing_saved_gives_null()
    {
        Assert.IsNull(await _database.GetAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task Save_then_get_and_save_again_overwrites()
    {
        var first = new SiteSettingsEntity { WelcomeText = "Hei", AboutText = "Om oss med æøå", HeroMode = HeroMode.Fixed, HeroImageId = Guid.NewGuid() };
        await _database.SaveAsync(first, CancellationToken.None);
        Assert.AreEqual(first, await _database.GetAsync(CancellationToken.None));

        var second = first with { HeroMode = HeroMode.Newest, HeroImageId = null };
        await _database.SaveAsync(second, CancellationToken.None);
        Assert.AreEqual(second, await _database.GetAsync(CancellationToken.None));
    }
}
