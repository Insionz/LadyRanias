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
        var allCats = await database.GetAllAsync(cancellationToken);

        // Kattungene vises i kullet sitt, ikke blant de voksne kattene.
        // (Del 2 bytter dette ut med Role, så en beholdt kattunge kan bli avlskatt.)
        return allCats
            .Where(cat => cat.LitterId == null)
            .OrderByDescending(cat => cat.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<CatDto>> GetByLitterAsync(Guid litterId, CancellationToken cancellationToken)
    {
        // Henter alle og filtrerer i minnet. Med noen titalls katter er det raskere enn det høres ut,
        // og enklere enn en egen indeks på LitterId.
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

        Validate(newKitten.Sex, newKitten.BirthDate, timeNow);

        var kitten = new CatEntity
        {
            Id = Guid.NewGuid(),
            Name = newKitten.Name,
            Sex = newKitten.Sex,
            Description = newKitten.Description,
            BirthDate = newKitten.BirthDate,
            CreatedAt = timeNow,
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

        if (cat.LitterId == null)
        {
            throw new ValidationException("Only kittens have a sale status");
        }

        var changed = cat with { SaleStatus = status };
        await database.SaveAsync(changed, cancellationToken);

        return ToDto(changed);
    }

    public async Task<CatDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var cat = await database.GetAsync(id, cancellationToken);

        return cat == null ? null : ToDto(cat);
    }

    public async Task<CatDto> CreateAsync(CreateCatDto newCat, CancellationToken cancellationToken)
    {
        var timeNow = timeProvider.GetUtcNow();

        Validate(newCat.Sex, newCat.BirthDate, timeNow);

        var cat = new CatEntity
        {
            Id = Guid.NewGuid(),
            Name = newCat.Name,
            Sex = newCat.Sex,
            Description = newCat.Description,
            BirthDate = newCat.BirthDate,
            CreatedAt = timeNow,
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
        Validate(update.Sex, update.BirthDate, timeProvider.GetUtcNow());

        // «with» lager en kopi med de nye verdiene. Id og CreatedAt følger med uendret.
        var updatedCat = cat with
        {
            Name = update.Name,
            Sex = update.Sex,
            BirthDate = update.BirthDate,
            Description = update.Description,
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

    private static void Validate(SexEnum sex, DateOnly birthDate, DateTimeOffset now)
    {
        if (birthDate > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new ValidationException("BirthDate must be today or before");
        }

        if (sex == SexEnum.Unknown)
        {
            throw new ValidationException("Sex must be either male or female");
        }
    }

    private static CatDto ToDto(CatEntity newCat)
    {
        return new CatDto
        {
            Id = newCat.Id,
            Name = newCat.Name,
            Sex = newCat.Sex,
            Description = newCat.Description,
            BirthDate = newCat.BirthDate,
            CreatedAt = newCat.CreatedAt,
            LitterId = newCat.LitterId,
            SaleStatus = newCat.SaleStatus,
        };
    }
}
