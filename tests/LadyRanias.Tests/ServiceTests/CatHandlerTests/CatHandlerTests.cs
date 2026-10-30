using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.Extensions.Time.Testing;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.Tests.ServiceTests.CatHandlerTests;

[TestClass]
public sealed class CatHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new FakeTimeProvider(Start);
    private readonly CatHandler _handler;
    private readonly InMemoryCatDatabase _database = new();
    private readonly List<CatEntity> _cats = new()
    {
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Kiara",
            Description = "Katt med sibirsk blod",
            Sex = SexEnum.Female,
            BirthDate = new DateOnly(2026, 03, 01),
            CreatedAt = Start,
        },
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Børre",
            Description = "Katt med norsk blod",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2026, 04, 01),
            CreatedAt = Start.AddMinutes(1),
        }
    };

    public CatHandlerTests()
    {
        _handler = new CatHandler(_database, _clock);
    }

    [TestInitialize]
    public async Task OnSetupAsync()
    {
        foreach (var cat in _cats)
        {
            await _database.SaveAsync(cat, CancellationToken.None);
        }
    }

    [TestMethod]
    public async Task Get_all_cats_returns_all_newest_first()
    {
        var result = await _handler.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, result);
        Assert.AreEqual("Børre", result[0].Name);
        Assert.AreEqual("Kiara", result[1].Name);

    }

    [TestMethod]
    public async Task Get_cat_returns_cat()
    {
        var cat = _cats.First();

        var result = await _handler.GetAsync(cat.Id, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(cat.Id, result.Id);
        Assert.AreEqual(cat.Name, result.Name);
        Assert.AreEqual(cat.Sex, result.Sex);
        Assert.AreEqual(cat.BirthDate, result.BirthDate);
    }

    [TestMethod]
    public async Task Delete_cat_returns_true()
    {

        var cat = _cats.First();

        var result = await _handler.DeleteAsync(cat.Id, CancellationToken.None);

        var catAfterDelete = await _handler.GetAsync(cat.Id, CancellationToken.None);

        Assert.IsTrue(result);
        Assert.IsNull(catAfterDelete);
    }

    [TestMethod]
    public async Task Create_sets_CreatedAt_from_clock()
    {
        var createdCat = await _handler.CreateAsync(new CreateCatDto
        {
            Name = "Test",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2025, 05, 05),
            Description = "Test"
        }, CancellationToken.None);

        Assert.AreEqual(Start, createdCat.CreatedAt);
    }

    [TestMethod]
    public async Task Get_unknown_id_returns_null()
    {
        var result = await _handler.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task Delete_unknown_id_returns_false()
    {
        var deleted = await _handler.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public async Task Create_with_future_birthdate_is_rejected()
    {
        var birthDate = DateOnly.FromDateTime(Start.UtcDateTime).AddDays(1);

        var catBornTomorrow = new CreateCatDto
        {
            Name = "Test",
            Sex = SexEnum.Male,
            BirthDate = birthDate,
            Description = "Test"
        };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(catBornTomorrow, CancellationToken.None));
    }

    [TestMethod]
    public async Task Create_with_unknown_sex_is_rejected()
    {
        var birthDate = DateOnly.FromDateTime(Start.UtcDateTime).AddDays(-30);

        var catBornTomorrow = new CreateCatDto
        {
            Name = "Test",
            Sex = SexEnum.Unknown,
            BirthDate = birthDate,
            Description = "Test"
        };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(catBornTomorrow, CancellationToken.None));
    }

    [TestMethod]
    public async Task Create_with_birthdate_today_is_accepted()
    {
        var birthDateToday = DateOnly.FromDateTime(Start.UtcDateTime);

        var catBornToday = new CreateCatDto
        {
            Name = "Test",
            Sex = SexEnum.Male,
            BirthDate = birthDateToday,
            Description = "Test"
        };

        var response = await _handler.CreateAsync(catBornToday, CancellationToken.None);

        Assert.AreEqual(birthDateToday, response.BirthDate);
    }
}
