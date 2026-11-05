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

        return allCats.OrderByDescending(cat => cat.CreatedAt).Select(ToDto).ToList();
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
        };
    }
}
