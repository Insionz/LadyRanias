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
    private readonly RecordingListener _listener = new();
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
        _handler = new CatHandler(_database, _clock, [_listener]);
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
            Breed = Breed.MaineCoon,
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
            Breed = Breed.MaineCoon,
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
            Breed = Breed.MaineCoon,
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
            Breed = Breed.MaineCoon,
            Name = "Test",
            Sex = SexEnum.Male,
            BirthDate = birthDateToday,
            Description = "Test"
        };

        var response = await _handler.CreateAsync(catBornToday, CancellationToken.None);

        Assert.AreEqual(birthDateToday, response.BirthDate);
    }

    [TestMethod]
    public async Task Delete_existing_cat_notifies_listeners()
    {
        var cat = _cats.First();

        await _handler.DeleteAsync(cat.Id, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { cat.Id }, _listener.DeletedCatIds);
    }

    [TestMethod]
    public async Task Delete_unknown_cat_does_not_notify_listeners()
    {
        await _handler.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.HasCount(0, _listener.DeletedCatIds);
    }

    private static UpdateCatDto ValidUpdate() => new()
    {
        Breed = Breed.MaineCoon,
        Name = "Kiara Nova",
        Sex = SexEnum.Female,
        BirthDate = new DateOnly(2026, 2, 15),
        Description = "Ny beskrivelse",
    };

    [TestMethod]
    public async Task Update_changes_all_fields_but_keeps_id_and_created_at()
    {
        var cat = _cats.First();

        var updated = await _handler.UpdateAsync(cat.Id, ValidUpdate(), CancellationToken.None);

        Assert.IsNotNull(updated);
        Assert.AreEqual(cat.Id, updated.Id);
        Assert.AreEqual(cat.CreatedAt, updated.CreatedAt);
        Assert.AreEqual("Kiara Nova", updated.Name);
        Assert.AreEqual(new DateOnly(2026, 2, 15), updated.BirthDate);
        Assert.AreEqual("Ny beskrivelse", updated.Description);

        // Og den er faktisk lagret, ikke bare returnert.
        var fetched = await _handler.GetAsync(cat.Id, CancellationToken.None);
        Assert.AreEqual(updated, fetched);
    }

    [TestMethod]
    public async Task Update_with_future_birthdate_is_rejected_and_nothing_is_saved()
    {
        var cat = _cats.First();
        var update = ValidUpdate() with { BirthDate = DateOnly.FromDateTime(Start.UtcDateTime).AddDays(1) };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(cat.Id, update, CancellationToken.None));

        var fetched = await _handler.GetAsync(cat.Id, CancellationToken.None);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(cat.Name, fetched.Name);
    }

    [TestMethod]
    public async Task Update_with_unknown_sex_is_rejected()
    {
        var update = ValidUpdate() with { Sex = SexEnum.Unknown };

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(_cats.First().Id, update, CancellationToken.None));
    }

    [TestMethod]
    public async Task Update_unknown_id_returns_null()
    {
        var result = await _handler.UpdateAsync(Guid.NewGuid(), ValidUpdate(), CancellationToken.None);

        Assert.IsNull(result);
    }

    // ---------- Kattunger ----------

    private static CreateCatDto NewKitten(string name) => new()
    {
        Breed = Breed.MaineCoon,
        Name = name,
        Sex = SexEnum.Female,
        BirthDate = new DateOnly(2026, 10, 1),
        Description = string.Empty,
    };

    [TestMethod]
    public async Task Kittens_are_not_in_get_all_but_in_get_by_litter_oldest_first()
    {
        var litterId = Guid.NewGuid();
        var first = await _handler.CreateKittenAsync(litterId, NewKitten("Ask"), CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _handler.CreateKittenAsync(litterId, NewKitten("Embla"), CancellationToken.None);
        await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("I et annet kull"), CancellationToken.None);

        var all = await _handler.GetAllAsync(CancellationToken.None);
        Assert.HasCount(2, all); // bare Kiara og Børre

        var inLitter = await _handler.GetByLitterAsync(litterId, CancellationToken.None);
        Assert.HasCount(2, inLitter);
        Assert.AreEqual(first.Id, inLitter[0].Id);
        Assert.AreEqual(second.Id, inLitter[1].Id);
    }

    [TestMethod]
    public async Task Create_kitten_is_available_and_belongs_to_litter()
    {
        var litterId = Guid.NewGuid();

        var kitten = await _handler.CreateKittenAsync(litterId, NewKitten("Ask"), CancellationToken.None);

        Assert.AreEqual(litterId, kitten.LitterId);
        Assert.AreEqual(KittenStatus.Available, kitten.SaleStatus);
        Assert.AreEqual(kitten, await _handler.GetAsync(kitten.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Create_kitten_with_unknown_sex_is_rejected()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask") with { Sex = SexEnum.Unknown }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Set_sale_status_changes_kitten()
    {
        var kitten = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask"), CancellationToken.None);

        var reserved = await _handler.SetSaleStatusAsync(kitten.Id, KittenStatus.Reserved, CancellationToken.None);

        Assert.IsNotNull(reserved);
        Assert.AreEqual(KittenStatus.Reserved, reserved.SaleStatus);
    }

    [TestMethod]
    public async Task Set_sale_status_on_adult_cat_is_rejected()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.SetSaleStatusAsync(_cats[0].Id, KittenStatus.Sold, CancellationToken.None));
    }

    [TestMethod]
    public async Task Set_sale_status_on_unknown_cat_returns_null()
    {
        Assert.IsNull(await _handler.SetSaleStatusAsync(Guid.NewGuid(), KittenStatus.Sold, CancellationToken.None));
    }

    [TestMethod]
    public async Task Update_kitten_keeps_litter_and_sale_status()
    {
        var kitten = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask"), CancellationToken.None);

        var updated = await _handler.UpdateAsync(kitten.Id, new UpdateCatDto
        {
            Breed = Breed.MaineCoon,
            Role = CatRole.Kitten,
            Name = "Ask Nytt Navn",
            Sex = kitten.Sex,
            BirthDate = kitten.BirthDate,
            Description = "Endret",
        }, CancellationToken.None);

        Assert.IsNotNull(updated);
        Assert.AreEqual(kitten.LitterId, updated.LitterId);
        Assert.AreEqual(KittenStatus.Available, updated.SaleStatus);
    }

    // ---------- Rase, EMS og rolle (steg 14 del 2) ----------

    private static CreateCatDto NewAdult() => new()
    {
        Breed = Breed.SacredBirman,
        Name = "Mira",
        Sex = SexEnum.Female,
        BirthDate = new DateOnly(2024, 5, 1),
        Description = "Birma",
    };

    [TestMethod]
    public async Task Create_without_breed_is_rejected()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { Breed = Breed.Unknown }, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("SBI a 21")]
    [DataRow("sbi n")]          // små bokstaver er greit
    [DataRow("  SBI   n  ")]    // mellomrom ryddes bort
    [DataRow("")]               // valgfri
    [DataRow(null)]
    public async Task Ems_code_matching_breed_is_accepted(string? ems)
    {
        var cat = await _handler.CreateAsync(NewAdult() with { EmsCode = ems }, CancellationToken.None);

        Assert.AreEqual(Breed.SacredBirman, cat.Breed);
    }

    [TestMethod]
    public async Task Ems_code_is_cleaned_and_empty_text_becomes_null()
    {
        var cat = await _handler.CreateAsync(NewAdult() with { EmsCode = "  SBI   n  ", Color = "   " }, CancellationToken.None);

        Assert.AreEqual("SBI n", cat.EmsCode);
        Assert.IsNull(cat.Color);
    }

    [TestMethod]
    [DataRow("MCO n 22")]   // feil rase
    [DataRow("SBIX n")]     // ikke en rasekode
    [DataRow("n 21")]       // rasekoden mangler
    public async Task Ems_code_for_another_breed_is_rejected(string ems)
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { EmsCode = ems }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Create_as_kitten_is_rejected()
    {
        // Kattunger lages via kullet, så de får LitterId.
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { Role = CatRole.Kitten }, CancellationToken.None));
    }

    [TestMethod]
    public async Task External_cat_is_in_admin_list_but_not_among_our_own()
    {
        var external = await _handler.CreateAsync(NewAdult() with { Role = CatRole.External, Sex = SexEnum.Male }, CancellationToken.None);
        var retired = await _handler.CreateAsync(NewAdult() with { Role = CatRole.Retired }, CancellationToken.None);

        var all = await _handler.GetAllAsync(CancellationToken.None);
        var own = await _handler.GetOwnAsync(CancellationToken.None);

        Assert.IsTrue(all.Any(c => c.Id == external.Id));
        Assert.IsFalse(own.Any(c => c.Id == external.Id));
        Assert.IsTrue(own.Any(c => c.Id == retired.Id));
    }

    private static UpdateCatDto UpdateFrom(CatDto cat, CatRole role) => new()
    {
        Name = cat.Name,
        Sex = cat.Sex,
        BirthDate = cat.BirthDate,
        Description = cat.Description,
        Breed = cat.Breed,
        Role = role,
    };

    [TestMethod]
    public async Task Adult_cat_cannot_become_kitten()
    {
        var cat = await _handler.CreateAsync(NewAdult(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(cat.Id, UpdateFrom(cat, CatRole.Kitten), CancellationToken.None));
    }

    [TestMethod]
    public async Task Kitten_that_is_not_kept_cannot_become_breeding_cat()
    {
        var kitten = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask"), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(kitten.Id, UpdateFrom(kitten, CatRole.Breeding), CancellationToken.None));
    }

    [TestMethod]
    public async Task Kept_kitten_becomes_breeding_cat_and_shows_among_our_cats()
    {
        var litterId = Guid.NewGuid();
        var kitten = await _handler.CreateKittenAsync(litterId, NewKitten("Ask"), CancellationToken.None);
        var kept = await _handler.SetSaleStatusAsync(kitten.Id, KittenStatus.Kept, CancellationToken.None);
        Assert.IsNotNull(kept);

        var grownUp = await _handler.UpdateAsync(kitten.Id, UpdateFrom(kept, CatRole.Breeding), CancellationToken.None);

        Assert.IsNotNull(grownUp);
        Assert.AreEqual(CatRole.Breeding, grownUp.Role);
        Assert.AreEqual(litterId, grownUp.LitterId); // husker fortsatt hvor den er født
        Assert.IsTrue((await _handler.GetOwnAsync(CancellationToken.None)).Any(c => c.Id == kitten.Id));
        Assert.HasCount(1, await _handler.GetByLitterAsync(litterId, CancellationToken.None)); // og vises fortsatt i kullet

        // Nå er den ingen kattunge lenger, så den har ingen salgsstatus å endre.
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.SetSaleStatusAsync(kitten.Id, KittenStatus.Available, CancellationToken.None));
    }

    // ---------- Døde katter og minnesiden ----------

    [TestMethod]
    public async Task Deceased_cat_leaves_our_cats_and_shows_in_memorial_latest_first()
    {
        var mira = await _handler.CreateAsync(NewAdult() with { Name = "Mira", DeathDate = new DateOnly(2025, 3, 1) }, CancellationToken.None);
        var tara = await _handler.CreateAsync(NewAdult() with { Name = "Tara", DeathDate = new DateOnly(2026, 6, 1) }, CancellationToken.None);

        var own = await _handler.GetOwnAsync(CancellationToken.None);
        var memorial = await _handler.GetInMemoryAsync(CancellationToken.None);

        Assert.IsFalse(own.Any(c => c.Id == mira.Id || c.Id == tara.Id));
        Assert.HasCount(2, memorial);
        Assert.AreEqual(tara.Id, memorial[0].Id); // sist døde først
        Assert.AreEqual(mira.Id, memorial[1].Id);

        // Admin ser dem fortsatt, og de kan fortsatt slås opp, for eksempel som mor i et gammelt kull.
        Assert.HasCount(4, await _handler.GetAllAsync(CancellationToken.None));
        Assert.IsNotNull(await _handler.GetAsync(mira.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Cat_can_be_created_as_deceased_without_date_and_comes_last_in_memorial()
    {
        // Mange av de gamle kattene husker man ikke dødsdatoen til.
        var withDate = await _handler.CreateAsync(NewAdult() with { Name = "Mira", DeathDate = new DateOnly(2025, 3, 1) }, CancellationToken.None);
        var withoutDate = await _handler.CreateAsync(NewAdult() with { Name = "Gamle Tussi", Deceased = true }, CancellationToken.None);

        Assert.IsTrue(withoutDate.Deceased);
        Assert.IsNull(withoutDate.DeathDate);
        Assert.IsTrue(withDate.Deceased); // en dato betyr død, også uten avkrysning

        var memorial = await _handler.GetInMemoryAsync(CancellationToken.None);
        Assert.HasCount(2, memorial);
        Assert.AreEqual(withDate.Id, memorial[0].Id);
        Assert.AreEqual(withoutDate.Id, memorial[1].Id);
        Assert.IsFalse((await _handler.GetOwnAsync(CancellationToken.None)).Any(c => c.Id == withoutDate.Id));
    }

    [TestMethod]
    public async Task Unchecking_deceased_removes_the_date_too()
    {
        var cat = await _handler.CreateAsync(NewAdult() with { DeathDate = new DateOnly(2025, 3, 1) }, CancellationToken.None);

        // Satt på feil katt. Mamma fjerner avkrysningen, og skjemaet sender ingen dato.
        var alive = await _handler.UpdateAsync(cat.Id, UpdateFrom(cat, cat.Role) with { Deceased = false, DeathDate = null }, CancellationToken.None);

        Assert.IsNotNull(alive);
        Assert.IsFalse(alive.Deceased);
        Assert.IsNull(alive.DeathDate);
    }

    [TestMethod]
    public async Task Neutered_cat_cannot_be_in_breeding()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { Role = CatRole.Breeding, Neutered = true }, CancellationToken.None));

        var cat = await _handler.CreateAsync(NewAdult(), CancellationToken.None);
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(cat.Id, UpdateFrom(cat, CatRole.Breeding) with { Neutered = true }, CancellationToken.None));
    }

    [TestMethod]
    [DataRow(CatRole.Retired)]
    [DataRow(CatRole.External)]
    public async Task Neutered_is_fine_for_other_roles(CatRole role)
    {
        var cat = await _handler.CreateAsync(NewAdult() with { Role = role, Neutered = true }, CancellationToken.None);

        Assert.IsTrue(cat.Neutered);
    }

    [TestMethod]
    public async Task Neutered_kitten_is_fine()
    {
        // Noen kastrerer kattunger som selges som familiekatter, før de flytter.
        var kitten = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask"), CancellationToken.None);

        var neutered = await _handler.UpdateAsync(kitten.Id, UpdateFrom(kitten, CatRole.Kitten) with { Neutered = true }, CancellationToken.None);

        Assert.IsTrue(neutered?.Neutered);
    }

    // ---------- Mor og far for kjøpte katter ----------

    [TestMethod]
    public async Task Bought_cat_can_have_parents_in_the_system_or_as_names()
    {
        var stud = await _handler.CreateAsync(NewAdult() with { Name = "Lånt Balder", Sex = SexEnum.Male, Role = CatRole.External }, CancellationToken.None);

        var cat = await _handler.CreateAsync(NewAdult() with
        {
            FatherId = stud.Id,
            FatherName = "Ignoreres, for id-en vinner",
            MotherName = "  IC NO*Skogheim's Frøya  ",
        }, CancellationToken.None);

        Assert.AreEqual(stud.Id, cat.FatherId);
        Assert.IsNull(cat.FatherName);
        Assert.IsNull(cat.MotherId);
        Assert.AreEqual("IC NO*Skogheim's Frøya", cat.MotherName);
    }

    [TestMethod]
    public async Task Parent_must_exist_and_have_the_right_sex()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { MotherId = Guid.NewGuid() }, CancellationToken.None));

        // Kiara er hunnkatt, og kan ikke være far.
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { FatherId = _cats[0].Id }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Cat_cannot_be_its_own_parent()
    {
        var cat = await _handler.CreateAsync(NewAdult(), CancellationToken.None);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.UpdateAsync(cat.Id, UpdateFrom(cat, cat.Role) with { MotherId = cat.Id }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Kept_kitten_is_among_our_cats_before_its_role_is_changed()
    {
        var kitten = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Ask"), CancellationToken.None);
        var underEvaluation = await _handler.CreateKittenAsync(Guid.NewGuid(), NewKitten("Embla"), CancellationToken.None, KittenStatus.UnderEvaluation);

        Assert.IsFalse((await _handler.GetOwnAsync(CancellationToken.None)).Any(c => c.Id == kitten.Id));

        await _handler.SetSaleStatusAsync(kitten.Id, KittenStatus.Kept, CancellationToken.None);

        var own = await _handler.GetOwnAsync(CancellationToken.None);
        Assert.IsTrue(own.Any(c => c.Id == kitten.Id));
        Assert.IsFalse(own.Any(c => c.Id == underEvaluation.Id)); // ikke bestemt ennå
    }

    [TestMethod]
    public async Task Death_before_birth_is_rejected()
    {
        var cat = NewAdult();

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(cat with { DeathDate = cat.BirthDate.AddDays(-1) }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Death_in_the_future_is_rejected()
    {
        var tomorrow = DateOnly.FromDateTime(Start.UtcDateTime).AddDays(1);

        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewAdult() with { DeathDate = tomorrow }, CancellationToken.None));
    }

    [TestMethod]
    public async Task Update_can_set_and_clear_death_date()
    {
        var cat = await _handler.CreateAsync(NewAdult(), CancellationToken.None);
        var died = new DateOnly(2026, 1, 1);

        var deceased = await _handler.UpdateAsync(cat.Id, UpdateFrom(cat, cat.Role) with { DeathDate = died }, CancellationToken.None);
        Assert.AreEqual(died, deceased?.DeathDate);

        // Feil katt? Mamma kan angre.
        var alive = await _handler.UpdateAsync(cat.Id, UpdateFrom(cat, cat.Role) with { DeathDate = null }, CancellationToken.None);
        Assert.IsNull(alive?.DeathDate);
    }

    /// <summary>Husker hvilke katter den har fått beskjed om, så testen kan sjekke at CatHandler sier fra.</summary>
    private sealed class RecordingListener : ICatDeletedListener
    {
        public List<Guid> DeletedCatIds { get; } = [];

        public Task OnCatDeletedAsync(Guid catId, CancellationToken cancellationToken)
        {
            DeletedCatIds.Add(catId);
            return Task.CompletedTask;
        }
    }
}
