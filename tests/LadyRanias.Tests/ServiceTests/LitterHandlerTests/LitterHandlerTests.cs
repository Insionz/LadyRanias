using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Litters.Impl;
using Microsoft.Extensions.Time.Testing;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.Tests.ServiceTests.LitterHandlerTests;

[TestClass]
public sealed class LitterHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Start.UtcDateTime);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryLitterDatabase _litters = new();
    private readonly CatHandler _cats;
    private readonly LitterHandler _handler;

    private CatDto _mother = null!;
    private CatDto _father = null!;

    public LitterHandlerTests()
    {
        // Ekte CatHandler med database i minnet. Da tester vi samspillet med Cats uten å lage en falsk ICatHandler.
        _cats = new CatHandler(new InMemoryCatDatabase(), _clock, []);
        _handler = new LitterHandler(_litters, _cats, _clock);
    }

    [TestInitialize]
    public async Task CreateParentsAsync()
    {
        _mother = await _cats.CreateAsync(NewCat("Kiara", SexEnum.Female), CancellationToken.None);
        _father = await _cats.CreateAsync(NewCat("Børre", SexEnum.Male), CancellationToken.None);
    }

    private static CreateCatDto NewCat(string name, SexEnum sex) => new()
    {
        Name = name,
        Sex = sex,
        BirthDate = new DateOnly(2022, 1, 1),
        Description = "Forelder",
    };

    private CreateLitterDto NewLitter() => new()
    {
        Name = "A-kullet",
        MotherId = _mother.Id,
        FatherId = _father.Id,
        ExpectedDate = Today.AddDays(30),
    };

    private static CreateKittenDto NewKitten(string name = "Ask") => new()
    {
        Name = name,
        Sex = SexEnum.Male,
    };

    private async Task<LitterDto> CreateBornLitterAsync()
    {
        var litter = await _handler.CreateAsync(NewLitter(), CancellationToken.None);
        var born = await _handler.MarkBornAsync(litter.Id, new MarkBornDto { BirthDate = Today }, CancellationToken.None);
        Assert.IsNotNull(born);
        return born;
    }

    // ---------- Opprette kull ----------

    [TestMethod]
    public async Task Create_litter_is_planned_without_kittens()
    {
        var litter = await _handler.CreateAsync(NewLitter(), CancellationToken.None);

        Assert.AreEqual(LitterStatus.Planned, litter.Status);
        Assert.IsNull(litter.BirthDate);
        Assert.HasCount(0, litter.Kittens);
        Assert.AreEqual(Start, litter.CreatedAt);
    }

    [TestMethod]
    public async Task Create_with_unknown_mother_is_rejected()
    {
        var litter = NewLitter() with { MotherId = Guid.NewGuid() };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(litter, CancellationToken.None));
    }

    [TestMethod]
    public async Task Create_with_mother_and_father_swapped_is_rejected()
    {
        var litter = NewLitter() with { MotherId = _father.Id, FatherId = _mother.Id };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(litter, CancellationToken.None));
    }

    // ---------- Fødsel ----------

    [TestMethod]
    public async Task Mark_born_sets_status_and_birth_date()
    {
        var litter = await _handler.CreateAsync(NewLitter(), CancellationToken.None);

        var born = await _handler.MarkBornAsync(litter.Id, new MarkBornDto { BirthDate = Today }, CancellationToken.None);

        Assert.IsNotNull(born);
        Assert.AreEqual(LitterStatus.Born, born.Status);
        Assert.AreEqual(Today, born.BirthDate);
    }

    [TestMethod]
    public async Task Mark_born_in_the_future_is_rejected()
    {
        var litter = await _handler.CreateAsync(NewLitter(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.MarkBornAsync(litter.Id, new MarkBornDto { BirthDate = Today.AddDays(1) }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Mark_born_twice_is_invalid_state()
    {
        var litter = await CreateBornLitterAsync();

        await Assert.ThrowsExactlyAsync<InvalidStateException>(
            () => _handler.MarkBornAsync(litter.Id, new MarkBornDto { BirthDate = Today }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Mark_born_unknown_litter_returns_null()
    {
        var result = await _handler.MarkBornAsync(Guid.NewGuid(), new MarkBornDto { BirthDate = Today }, CancellationToken.None);

        Assert.IsNull(result);
    }

    // ---------- Kattunger ----------

    [TestMethod]
    public async Task Add_kitten_to_planned_litter_is_invalid_state()
    {
        var litter = await _handler.CreateAsync(NewLitter(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<InvalidStateException>(
            () => _handler.AddKittenAsync(litter.Id, NewKitten(), CancellationToken.None));
    }

    [TestMethod]
    public async Task Add_kitten_to_born_litter_is_available_and_shows_on_litter()
    {
        var litter = await CreateBornLitterAsync();

        var kitten = await _handler.AddKittenAsync(litter.Id, NewKitten(), CancellationToken.None);

        Assert.IsNotNull(kitten);
        Assert.AreEqual(KittenStatus.Available, kitten.Status);

        var fetched = await _handler.GetAsync(litter.Id, CancellationToken.None);
        Assert.IsNotNull(fetched);
        Assert.HasCount(1, fetched.Kittens);
        Assert.AreEqual(kitten, fetched.Kittens[0]);
    }

    [TestMethod]
    public async Task Kitten_is_a_cat_with_the_birth_date_of_the_litter()
    {
        var litter = await CreateBornLitterAsync();

        var kitten = await _handler.AddKittenAsync(litter.Id, NewKitten(), CancellationToken.None);
        Assert.IsNotNull(kitten);

        // Samme Id i Cats. Det er det som gjør at bildene virker for kattunger.
        var cat = await _cats.GetAsync(kitten.Id, CancellationToken.None);
        Assert.IsNotNull(cat);
        Assert.AreEqual(litter.Id, cat.LitterId);
        Assert.AreEqual(Today, cat.BirthDate);
        Assert.AreEqual(KittenStatus.Available, cat.SaleStatus);

        // Men den vises ikke blant de voksne kattene.
        var adults = await _cats.GetAllAsync(CancellationToken.None);
        Assert.IsFalse(adults.Any(c => c.Id == kitten.Id));
    }

    [TestMethod]
    public async Task Kitten_cannot_be_parent_of_a_litter()
    {
        var litter = await CreateBornLitterAsync();
        var kitten = await _handler.AddKittenAsync(litter.Id, NewKitten("Ask") with { Sex = SexEnum.Male }, CancellationToken.None);
        Assert.IsNotNull(kitten);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewLitter() with { FatherId = kitten.Id }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Kitten_in_another_litter_is_not_found()
    {
        var litterA = await CreateBornLitterAsync();
        var litterB = await CreateBornLitterAsync();
        var kitten = await _handler.AddKittenAsync(litterA.Id, NewKitten(), CancellationToken.None);
        Assert.IsNotNull(kitten);

        // Riktig kattunge, men feil kull i adressen.
        Assert.IsNull(await _handler.ChangeKittenStatusAsync(litterB.Id, kitten.Id, new() { Status = KittenStatus.Sold }, CancellationToken.None));
        Assert.IsFalse(await _handler.DeleteKittenAsync(litterB.Id, kitten.Id, CancellationToken.None));

        // En voksen katt er heller ikke en kattunge i kullet.
        Assert.IsNull(await _handler.ChangeKittenStatusAsync(litterA.Id, _mother.Id, new() { Status = KittenStatus.Sold }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Delete_kitten_removes_the_cat()
    {
        var litter = await CreateBornLitterAsync();
        var kitten = await _handler.AddKittenAsync(litter.Id, NewKitten(), CancellationToken.None);
        Assert.IsNotNull(kitten);

        Assert.IsTrue(await _handler.DeleteKittenAsync(litter.Id, kitten.Id, CancellationToken.None));

        Assert.IsNull(await _cats.GetAsync(kitten.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Add_kitten_with_unknown_sex_is_rejected()
    {
        var litter = await CreateBornLitterAsync();

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.AddKittenAsync(litter.Id, NewKitten() with { Sex = SexEnum.Unknown }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Add_kitten_to_unknown_litter_returns_null()
    {
        var kitten = await _handler.AddKittenAsync(Guid.NewGuid(), NewKitten(), CancellationToken.None);

        Assert.IsNull(kitten);
    }

    [TestMethod]
    public async Task Reserve_then_sell_works_but_sold_kitten_cannot_be_reserved()
    {
        var litter = await CreateBornLitterAsync();
        var kitten = await _handler.AddKittenAsync(litter.Id, NewKitten(), CancellationToken.None);
        Assert.IsNotNull(kitten);

        var reserved = await _handler.ChangeKittenStatusAsync(litter.Id, kitten.Id, new() { Status = KittenStatus.Reserved }, CancellationToken.None);
        Assert.IsNotNull(reserved);
        Assert.AreEqual(KittenStatus.Reserved, reserved.Status);

        var sold = await _handler.ChangeKittenStatusAsync(litter.Id, kitten.Id, new() { Status = KittenStatus.Sold }, CancellationToken.None);
        Assert.IsNotNull(sold);
        Assert.AreEqual(KittenStatus.Sold, sold.Status);

        await Assert.ThrowsExactlyAsync<InvalidStateException>(
            () => _handler.ChangeKittenStatusAsync(litter.Id, kitten.Id, new() { Status = KittenStatus.Reserved }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Change_status_of_unknown_kitten_returns_null()
    {
        var litter = await CreateBornLitterAsync();

        var result = await _handler.ChangeKittenStatusAsync(litter.Id, Guid.NewGuid(), new() { Status = KittenStatus.Sold }, CancellationToken.None);

        Assert.IsNull(result);
    }

    // ---------- Henting og sletting ----------

    [TestMethod]
    public async Task Get_all_returns_newest_first()
    {
        var first = await _handler.CreateAsync(NewLitter() with { Name = "A-kullet" }, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _handler.CreateAsync(NewLitter() with { Name = "B-kullet" }, CancellationToken.None);

        var all = await _handler.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, all);
        Assert.AreEqual(second.Id, all[0].Id);
        Assert.AreEqual(first.Id, all[1].Id);
    }

    [TestMethod]
    public async Task Delete_litter_also_deletes_its_kittens()
    {
        var litter = await CreateBornLitterAsync();
        await _handler.AddKittenAsync(litter.Id, NewKitten("Ask"), CancellationToken.None);
        await _handler.AddKittenAsync(litter.Id, NewKitten("Embla"), CancellationToken.None);

        var deleted = await _handler.DeleteAsync(litter.Id, CancellationToken.None);

        Assert.IsTrue(deleted);
        Assert.IsNull(await _handler.GetAsync(litter.Id, CancellationToken.None));
        Assert.HasCount(0, await _cats.GetByLitterAsync(litter.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Delete_unknown_litter_returns_false()
    {
        Assert.IsFalse(await _handler.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
