using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Litters.Impl;

// Litters bruker ICatHandler fra Cats/Api for å slå opp mor og far, og for kattungene.
// En kattunge er en vanlig katt med LitterId. Cats lagrer den. Litters vet hvilket kull den hører til,
// og hvilke statusendringer som er lov.
// Avhengigheten går bare én vei: Cats vet ingenting om Litters.
internal sealed class LitterHandler(
    ILitterDatabase litters,
    ICatHandler cats,
    TimeProvider timeProvider) : ILitterHandler
{
    public async Task<IReadOnlyList<LitterDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var all = await litters.GetAllAsync(cancellationToken);

        var result = new List<LitterDto>();
        foreach (var litter in all.OrderByDescending(l => l.CreatedAt))
        {
            result.Add(await ToDtoWithKittensAsync(litter, cancellationToken));
        }

        return result;
    }

    public async Task<LitterDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var litter = await litters.GetAsync(id, cancellationToken);

        return litter == null ? null : await ToDtoWithKittensAsync(litter, cancellationToken);
    }

    public async Task<LitterDto> CreateAsync(CreateLitterDto newLitter, CancellationToken cancellationToken)
    {
        await ValidateParentAsync(newLitter.MotherId, SexEnum.Female, "MotherId", cancellationToken);
        await ValidateParentAsync(newLitter.FatherId, SexEnum.Male, "FatherId", cancellationToken);

        var litter = new LitterEntity
        {
            Id = Guid.NewGuid(),
            Name = newLitter.Name,
            MotherId = newLitter.MotherId,
            FatherId = newLitter.FatherId,
            Status = LitterStatus.Planned,
            ExpectedDate = newLitter.ExpectedDate,
            BirthDate = null,
            Description = newLitter.Description,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await litters.SaveAsync(litter, cancellationToken);

        return ToLitterDto(litter, []);
    }

    public async Task<LitterDto?> MarkBornAsync(Guid id, MarkBornDto born, CancellationToken cancellationToken)
    {
        var litter = await litters.GetAsync(id, cancellationToken);
        if (litter == null) return null;

        // Først tilstanden (409), så input (400). Et kull som allerede er født, skal ikke fødes igjen,
        // uansett hvilken dato som sendes inn.
        if (litter.Status != LitterStatus.Planned)
        {
            throw new InvalidStateException("The litter is already born");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (born.BirthDate > today)
        {
            throw new ValidationException("BirthDate must be today or before");
        }

        var bornLitter = litter with
        {
            Status = LitterStatus.Born,
            BirthDate = born.BirthDate,
        };

        await litters.SaveAsync(bornLitter, cancellationToken);

        return await ToDtoWithKittensAsync(bornLitter, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        // Hent kattungene før kullet slettes, så vi vet hva som skal ryddes.
        var kittensInLitter = await cats.GetByLitterAsync(id, cancellationToken);

        var deleted = await litters.DeleteAsync(id, cancellationToken);
        if (!deleted) return false;

        // Samme rekkefølge som når en katt slettes: hovedraden først, så oppryddingen.
        // Kattungene slettes gjennom Cats, så bildene deres også ryddes bort (ICatDeletedListener).
        // CancellationToken.None: oppryddingen skal fullføres selv om forespørselen avbrytes.
        foreach (var kitten in kittensInLitter)
        {
            await cats.DeleteAsync(kitten.Id, CancellationToken.None);
        }

        return true;
    }

    public async Task<KittenDto?> AddKittenAsync(Guid litterId, CreateKittenDto newKitten, CancellationToken cancellationToken)
    {
        var litter = await litters.GetAsync(litterId, cancellationToken);
        if (litter == null) return null;

        if (litter.Status != LitterStatus.Born || litter.BirthDate == null)
        {
            throw new InvalidStateException("Kittens can only be added to a litter that is born");
        }

        // Kattungen får fødselsdatoen til kullet. Cats sjekker kjønn og dato, som for alle andre katter.
        var kitten = await cats.CreateKittenAsync(litterId, new CreateCatDto
        {
            Name = newKitten.Name,
            Sex = newKitten.Sex,
            BirthDate = litter.BirthDate.Value,
            Description = newKitten.Description,
        }, cancellationToken);

        return ToKittenDto(kitten, litterId);
    }

    public async Task<KittenDto?> ChangeKittenStatusAsync(Guid litterId, Guid kittenId, ChangeKittenStatusDto change, CancellationToken cancellationToken)
    {
        var kitten = await FindKittenAsync(litterId, kittenId, cancellationToken);
        if (kitten == null) return null;

        var current = kitten.SaleStatus ?? KittenStatus.Available;
        if (!KittenStatusRules.CanChange(current, change.Status))
        {
            throw new InvalidStateException($"Cannot change kitten status from {current} to {change.Status}");
        }

        var changed = await cats.SetSaleStatusAsync(kittenId, change.Status, cancellationToken);

        return changed == null ? null : ToKittenDto(changed, litterId);
    }

    public async Task<bool> DeleteKittenAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        var kitten = await FindKittenAsync(litterId, kittenId, cancellationToken);
        if (kitten == null) return false;

        return await cats.DeleteAsync(kittenId, cancellationToken);
    }

    /// <summary>
    /// Katten, men bare hvis den er en kattunge i akkurat dette kullet.
    /// Ellers kunne man endret en voksen katt, eller en kattunge i et annet kull, via feil adresse.
    /// </summary>
    private async Task<CatDto?> FindKittenAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        var cat = await cats.GetAsync(kittenId, cancellationToken);

        return cat?.LitterId == litterId ? cat : null;
    }

    private async Task ValidateParentAsync(Guid catId, SexEnum expectedSex, string field, CancellationToken cancellationToken)
    {
        var cat = await cats.GetAsync(catId, cancellationToken);

        if (cat == null)
        {
            throw new ValidationException($"{field}: no cat with this id");
        }

        if (cat.Sex != expectedSex)
        {
            throw new ValidationException($"{field}: the cat must be {expectedSex.ToString().ToLowerInvariant()}");
        }

        // En kattunge kan ikke bli forelder. (Del 2: en beholdt kattunge som er blitt avlskatt, kan.)
        if (cat.LitterId != null)
        {
            throw new ValidationException($"{field}: the cat is a kitten");
        }
    }

    private async Task<LitterDto> ToDtoWithKittensAsync(LitterEntity litter, CancellationToken cancellationToken)
    {
        var kittensInLitter = await cats.GetByLitterAsync(litter.Id, cancellationToken);

        return ToLitterDto(litter, [.. kittensInLitter.Select(kitten => ToKittenDto(kitten, litter.Id))]);
    }

    private static LitterDto ToLitterDto(LitterEntity litter, IReadOnlyList<KittenDto> kittensInLitter)
    {
        return new LitterDto
        {
            Id = litter.Id,
            Name = litter.Name,
            MotherId = litter.MotherId,
            FatherId = litter.FatherId,
            Status = litter.Status,
            ExpectedDate = litter.ExpectedDate,
            BirthDate = litter.BirthDate,
            Description = litter.Description,
            CreatedAt = litter.CreatedAt,
            Kittens = kittensInLitter,
        };
    }

    private static KittenDto ToKittenDto(CatDto kitten, Guid litterId)
    {
        return new KittenDto
        {
            Id = kitten.Id,
            LitterId = litterId,
            Name = kitten.Name,
            Sex = kitten.Sex,
            Status = kitten.SaleStatus ?? KittenStatus.Available,
            Description = kitten.Description,
            CreatedAt = kitten.CreatedAt,
        };
    }
}
