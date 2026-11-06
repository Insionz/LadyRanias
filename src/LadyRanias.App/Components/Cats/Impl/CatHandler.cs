using LadyRanias.App.Components.Cats.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Cats.Impl;

internal sealed class CatHandler(
    ICatDatabase database,
    TimeProvider timeProvider,
    IEnumerable<ICatDeletedListener> deletedListeners) : ICatHandler
{
    public async Task<IReadOnlyList<CatDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        // Kattungene vises i kullet sitt. En beholdt kattunge som er blitt avlskatt, har ikke lenger Role = Kitten
        // og kommer med her.
        return await GetWhereAsync(cat => cat.Role != CatRole.Kitten, cancellationToken);
    }

    public async Task<IReadOnlyList<CatDto>> GetOwnAsync(CancellationToken cancellationToken)
    {
        return await GetWhereAsync(cat => IsOwn(cat) && !cat.Deceased, cancellationToken);
    }

    public async Task<IReadOnlyList<CatDto>> GetInMemoryAsync(CancellationToken cancellationToken)
    {
        var deceased = await GetWhereAsync(cat => IsOwn(cat) && cat.Deceased, cancellationToken);

        // Sist døde først. De uten dato (de vi ikke husker) til slutt, eldste først.
        return [.. deceased
            .OrderBy(cat => cat.DeathDate == null)
            .ThenByDescending(cat => cat.DeathDate)
            .ThenBy(cat => cat.BirthDate)];
    }

    /// <summary>Våre egne voksne katter. Ikke kattunger (de står i kullet) og ikke eksterne.</summary>
    private static bool IsOwn(CatEntity cat) => cat.Role is CatRole.Breeding or CatRole.Retired;

    private async Task<IReadOnlyList<CatDto>> GetWhereAsync(Func<CatEntity, bool> filter, CancellationToken cancellationToken)
    {
        var allCats = await database.GetAllAsync(cancellationToken);

        return allCats
            .Where(filter)
            .OrderByDescending(cat => cat.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<CatDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var cat = await database.GetAsync(id, cancellationToken);

        return cat == null ? null : ToDto(cat);
    }

    public async Task<CatDto> CreateAsync(CreateCatDto newCat, CancellationToken cancellationToken)
    {
        var timeNow = timeProvider.GetUtcNow();

        Validate(newCat.Sex, newCat.BirthDate, newCat.Breed, newCat.EmsCode, timeNow);
        ValidateDeathDate(newCat.BirthDate, newCat.DeathDate, timeNow);

        ValidateNeutered(newCat.Role, newCat.Neutered);

        if (newCat.Role == CatRole.Kitten)
        {
            throw new ValidationException("Role: kittens are added to a litter");
        }

        var cat = new CatEntity
        {
            Id = Guid.NewGuid(),
            Name = newCat.Name,
            Sex = newCat.Sex,
            Description = newCat.Description,
            BirthDate = newCat.BirthDate,
            CreatedAt = timeNow,
            Breed = newCat.Breed,
            Role = newCat.Role,
            EmsCode = Clean(newCat.EmsCode),
            Color = Clean(newCat.Color),
            Neutered = newCat.Neutered,
            RegistrationNumber = Clean(newCat.RegistrationNumber),
            Titles = Clean(newCat.Titles),
            Deceased = IsDeceased(newCat.Deceased, newCat.DeathDate),
            DeathDate = DeathDateIf(newCat.Deceased, newCat.DeathDate),
        };

        await database.SaveAsync(cat, cancellationToken);

        return ToDto(cat);
    }

    public async Task<CatDto?> UpdateAsync(Guid id, UpdateCatDto update, CancellationToken cancellationToken)
    {
        var cat = await database.GetAsync(id, cancellationToken);

        if (cat == null)
        {
            return null;
        }

        // Samme regler som ved opprettelse.
        Validate(update.Sex, update.BirthDate, update.Breed, update.EmsCode, timeProvider.GetUtcNow());
        ValidateDeathDate(update.BirthDate, update.DeathDate, timeProvider.GetUtcNow());
        ValidateRoleChange(cat, update.Role);
        ValidateNeutered(update.Role, update.Neutered);

        // «with» lager en kopi med de nye verdiene. Id, CreatedAt, LitterId og SaleStatus følger med uendret.
        var updatedCat = cat with
        {
            Name = update.Name,
            Sex = update.Sex,
            BirthDate = update.BirthDate,
            Description = update.Description,
            Breed = update.Breed,
            Role = update.Role,
            EmsCode = Clean(update.EmsCode),
            Color = Clean(update.Color),
            Neutered = update.Neutered,
            RegistrationNumber = Clean(update.RegistrationNumber),
            Titles = Clean(update.Titles),
            Deceased = IsDeceased(update.Deceased, update.DeathDate),
            DeathDate = DeathDateIf(update.Deceased, update.DeathDate),
        };

        await database.SaveAsync(updatedCat, cancellationToken);
        return ToDto(updatedCat);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await database.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return false;
        }

        // Katten slettes først. Feiler oppryddingen etterpå, blir det liggende bilder ingen ser,
        // i stedet for en katt som har mistet bildene sine.
        // CancellationToken.None: oppryddingen skal fullføres selv om forespørselen avbrytes.
        foreach (var listener in deletedListeners)
        {
            await listener.OnCatDeletedAsync(id, CancellationToken.None);
        }

        return true;
    }

    public async Task<IReadOnlyList<CatDto>> GetByLitterAsync(Guid litterId, CancellationToken cancellationToken)
    {
        // Henter alle og filtrerer i minnet. Med noen titalls katter er det raskere enn det høres ut,
        // og enklere enn en egen indeks på LitterId.
        // Alle som er født i kullet kommer med, også en som er blitt avlskatt. Kullet viser hele historien.
        var allCats = await database.GetAllAsync(cancellationToken);

        return allCats
            .Where(cat => cat.LitterId == litterId)
            .OrderBy(cat => cat.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<CatDto> CreateKittenAsync(Guid litterId, CreateCatDto newKitten, CancellationToken cancellationToken)
    {
        var timeNow = timeProvider.GetUtcNow();

        Validate(newKitten.Sex, newKitten.BirthDate, newKitten.Breed, newKitten.EmsCode, timeNow);

        var kitten = new CatEntity
        {
            Id = Guid.NewGuid(),
            Name = newKitten.Name,
            Sex = newKitten.Sex,
            Description = newKitten.Description,
            BirthDate = newKitten.BirthDate,
            CreatedAt = timeNow,
            Breed = newKitten.Breed,
            Role = CatRole.Kitten,
            EmsCode = Clean(newKitten.EmsCode),
            Color = Clean(newKitten.Color),
            Neutered = newKitten.Neutered,
            RegistrationNumber = Clean(newKitten.RegistrationNumber),
            Titles = Clean(newKitten.Titles),
            Deceased = false, // en ny kattunge lever
            DeathDate = null,
            LitterId = litterId,
            SaleStatus = KittenStatus.Available,
        };

        await database.SaveAsync(kitten, cancellationToken);

        return ToDto(kitten);
    }

    public async Task<CatDto?> SetSaleStatusAsync(Guid id, KittenStatus status, CancellationToken cancellationToken)
    {
        var cat = await database.GetAsync(id, cancellationToken);

        if (cat == null)
        {
            return null;
        }

        if (cat.Role != CatRole.Kitten)
        {
            throw new ValidationException("Only kittens have a sale status");
        }

        var changed = cat with { SaleStatus = status };
        await database.SaveAsync(changed, cancellationToken);

        return ToDto(changed);
    }

    /// <summary>Rasekoden i EMS. Første del av en EMS-kode, «MCO n 22».</summary>
    internal static string EmsBreedCode(Breed breed) => breed switch
    {
        Breed.MaineCoon => "MCO",
        Breed.SacredBirman => "SBI",
        _ => throw new ArgumentOutOfRangeException(nameof(breed), breed, "No EMS code"),
    };

    private static void Validate(SexEnum sex, DateOnly birthDate, Breed breed, string? emsCode, DateTimeOffset now)
    {
        if (birthDate > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new ValidationException("BirthDate must be today or before");
        }

        if (sex == SexEnum.Unknown)
        {
            throw new ValidationException("Sex must be either male or female");
        }

        if (breed == Breed.Unknown || !Enum.IsDefined(breed))
        {
            throw new ValidationException("Breed must be set");
        }

        // EMS-koden er valgfri (en Birma-kattunge har ikke fått fargen sin ennå),
        // men er den fylt ut, må den høre til rasen. «SBI a» på en Maine Coon er en skrivefeil.
        var ems = Clean(emsCode);
        if (ems != null)
        {
            var code = EmsBreedCode(breed);
            var firstPart = ems.Split(' ')[0];
            if (!firstPart.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"EmsCode must start with {code} for this breed");
            }
        }
    }

    /// <summary>En kastrert katt kan ikke få kattunger, så den kan ikke være i avl. Pensjonert og kastrert er det vanlige.</summary>
    private static void ValidateNeutered(CatRole role, bool neutered)
    {
        if (role == CatRole.Breeding && neutered)
        {
            throw new ValidationException("Neutered: a neutered cat cannot be in breeding. Set the role to Retired.");
        }
    }

    /// <summary>En dødsdato betyr at katten er død, selv om avkrysningen mangler.</summary>
    private static bool IsDeceased(bool deceased, DateOnly? deathDate) => deceased || deathDate != null;

    /// <summary>Datoen bare hvis katten er død. Fjernes avkrysningen igjen, forsvinner datoen også.</summary>
    private static DateOnly? DeathDateIf(bool deceased, DateOnly? deathDate) => IsDeceased(deceased, deathDate) ? deathDate : null;

    private static void ValidateDeathDate(DateOnly birthDate, DateOnly? deathDate, DateTimeOffset now)
    {
        if (deathDate == null)
        {
            return;
        }

        if (deathDate < birthDate)
        {
            throw new ValidationException("DeathDate cannot be before BirthDate");
        }

        if (deathDate > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new ValidationException("DeathDate must be today or before");
        }
    }

    /// <summary>
    /// Kitten → voksen: bare en beholdt kattunge kan bli avlskatt eller pensjonist. En solgt kattunge bor ikke her.
    /// Voksen → Kitten: bare en katt som er født i et kull hos oss.
    /// </summary>
    private static void ValidateRoleChange(CatEntity cat, CatRole newRole)
    {
        if (cat.Role == newRole)
        {
            return;
        }

        if (newRole == CatRole.Kitten && cat.LitterId == null)
        {
            throw new ValidationException("Role: only a cat born in a litter can be a kitten");
        }

        if (cat.Role == CatRole.Kitten && cat.SaleStatus != KittenStatus.Kept)
        {
            throw new ValidationException("Role: only a kept kitten can become an adult cat here");
        }
    }

    /// <summary>Fjerner mellomrom foran og bak, og doble mellomrom inni. Tom tekst blir null.</summary>
    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static CatDto ToDto(CatEntity cat)
    {
        return new CatDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Sex = cat.Sex,
            Description = cat.Description,
            BirthDate = cat.BirthDate,
            CreatedAt = cat.CreatedAt,
            Breed = cat.Breed,
            Role = cat.Role,
            EmsCode = cat.EmsCode,
            Color = cat.Color,
            Neutered = cat.Neutered,
            RegistrationNumber = cat.RegistrationNumber,
            Titles = cat.Titles,
            Deceased = cat.Deceased,
            DeathDate = cat.DeathDate,
            LitterId = cat.LitterId,
            SaleStatus = cat.SaleStatus,
        };
    }
}
