using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;

namespace LadyRanias.Tests.ServiceTests.CatHandlerTests;

[TestClass]
public sealed class CatHandlerTests
{
    private ICatHandler _handler = null!;
   

    [TestInitialize]
    public async Task OnSetupAsync()
    {
        _handler = new CatHandler(new InMemoryCatDatabase(), TimeProvider.System);

        await CreateMockDataAsync();
    }
    

    [TestMethod]
    public async Task TestGetAllCats()
    {

        var result = await _handler.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, result);
        Assert.AreEqual("Kiara", result[1].Name);
        Assert.AreEqual("Kiara 2", result[0].Name);

    }

    [TestMethod]
    public async Task TestGetCat()
    {
        var allCats = await _handler.GetAllAsync(CancellationToken.None);

        var cat = allCats[0];

        var result = await _handler.GetAsync(cat.Id, CancellationToken.None);

        Assert.AreEqual(cat, result);
    }

    [TestMethod]
    public async Task TestDeleteCat()
    {
        var allCats = await _handler.GetAllAsync(CancellationToken.None);

        var cat = allCats[0];

        var result = await _handler.DeleteAsync(cat.Id, CancellationToken.None);

        allCats = await _handler.GetAllAsync(CancellationToken.None);

        Assert.IsTrue(result);
        Assert.HasCount(1, allCats);
    }

    private async Task CreateMockDataAsync()
    {
        var cat = new CreateCatDto
        {
            Name = "Kiara",
            Description = "Katt med sibirsk blod",
            Sex = SexEnum.Female,
            BirthDate = new DateOnly(2026, 03, 01),
        };

        var cat2 = new CreateCatDto
        {
            Name = "Kiara 2",
            Description = "Katt med sibirsk blod 2 ",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2026, 03, 02),
        };

        var createdCat = await _handler.CreateAsync(cat, CancellationToken.None);
        var createdCat2 = await _handler.CreateAsync(cat2, CancellationToken.None);

        Console.WriteLine(createdCat);
        Console.WriteLine(createdCat2);
    }
}
