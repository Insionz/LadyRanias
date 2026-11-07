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
    TimeProvider timeProvider,
    IEnumerable<ILitterDeletedListener> deletedListeners) : ILitterHandler
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
        var mother = await ValidateParentAsync(newLitter.MotherId, SexEnum.Female, "MotherId", cancellationToken);
        var father = await ValidateParentAsync(newLitter.FatherId, SexEnum.Male, "FatherId", cancellationToken);

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

        return ToLitterDto(litter, [], mother, father);
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
        // En voksen katt som er født i kullet (beholdt), slettes IKKE. Den mister bare koblingen til kullet.
        // CancellationToken.None: oppryddingen skal fullføres selv om forespørselen avbrytes.
        foreach (var kitten in kittensInLitter)
        {
            if (kitten.Role == CatRole.Kitten)
            {
                await cats.DeleteAsync(kitten.Id, CancellationToken.None);
            }
            else
            {
                await cats.RemoveFromLitterAsync(kitten.Id, CancellationToken.None);
            }
        }

        // Bildene av hele kullet (CatImages).
        foreach (var listener in deletedListeners)
        {
            await listener.OnLitterDeletedAsync(id, CancellationToken.None);
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

        // Kattungen får rasen til moren. Maine Coon og Hellig Birma krysses ikke, så det er nok å se på henne.
        var mother = await cats.GetAsync(litter.MotherId, cancellationToken);
        if (mother == null || mother.Breed == Breed.Unknown)
        {
            throw new ValidationException("The mother has no breed. Set the breed on the mother first.");
        }

        // Kattungen får fødselsdatoen til kullet. Cats sjekker kjønn og dato, som for alle andre katter.
        var kitten = await cats.CreateKittenAsync(litterId, new CreateCatDto
        {
            Name = newKitten.Name,
            Sex = newKitten.Sex,
            BirthDate = litter.BirthDate.Value,
            Description = newKitten.Description,
            Breed = mother.Breed,
        }, cancellationToken, newKitten.Status);

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

        // En beholdt kattunge som er blitt voksen katt, er en av mammas katter. Den skal ikke slettes fra kullet.
        if (kitten.Role != CatRole.Kitten)
        {
            throw new InvalidStateException("This cat is one of our adult cats. Remove it from the litter instead of deleting it.");
        }

        return await cats.DeleteAsync(kittenId, cancellationToken);
    }

    public async Task<KittenDto?> LinkExistingCatAsync(Guid litterId, LinkCatDto link, CancellationToken cancellationToken)
    {
        var litter = await litters.GetAsync(litterId, cancellationToken);
        if (litter == null) return null;

        if (litter.Status != LitterStatus.Born || litter.BirthDate == null)
        {
            throw new InvalidStateException("Cats can only be linked to a litter that is born");
        }

        if (link.CatId == litter.MotherId || link.CatId == litter.FatherId)
        {
            throw new ValidationException("CatId: a parent cannot be a kitten in its own litter");
        }

        var cat = await cats.GetAsync(link.CatId, cancellationToken)
            ?? throw new ValidationException("CatId: no cat with this id");

        // Er fødselsdatoen en annen, vil vi at mamma skal se det og velge, ikke at vi endrer den i stillhet.
        if (cat.BirthDate != litter.BirthDate.Value && !link.UseLitterBirthDate)
        {
            throw new ValidationException(
                $"BirthDate: the cat is registered as born {cat.BirthDate:yyyy-MM-dd}, the litter {litter.BirthDate.Value:yyyy-MM-dd}");
        }

        var linked = await cats.AssignToLitterAsync(cat.Id, litterId, litter.BirthDate.Value, cancellationToken);

        return linked == null ? null : ToKittenDto(linked, litterId);
    }

    public async Task<bool> UnlinkCatAsync(Guid litterId, Guid catId, CancellationToken cancellationToken)
    {
        var cat = await FindKittenAsync(litterId, catId, cancellationToken);
        if (cat == null) return false;

        if (cat.Role == CatRole.Kitten)
        {
            throw new InvalidStateException("A kitten cannot be removed from its litter. Delete it instead.");
        }

        return await cats.RemoveFromLitterAsync(catId, cancellationToken) != null;
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

    private async Task<CatDto> ValidateParentAsync(Guid catId, SexEnum expectedSex, string field, CancellationToken cancellationToken)
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

        // En kattunge kan ikke bli forelder. En beholdt kattunge som er blitt avlskatt (Role = Breeding), kan.
        // En ekstern katt (lånt hannkatt fra et annet oppdrett) kan også.
        if (cat.Role == CatRole.Kitten)
        {
            throw new ValidationException($"{field}: the cat is a kitten");
        }

        return cat;
    }

    private async Task<LitterDto> ToDtoWithKittensAsync(LitterEntity litter, CancellationToken cancellationToken)
    {
        var kittensInLitter = await cats.GetByLitterAsync(litter.Id, cancellationToken);
        var mother = await cats.GetAsync(litter.MotherId, cancellationToken);
        var father = await cats.GetAsync(litter.FatherId, cancellationToken);

        return ToLitterDto(litter, [.. kittensInLitter.Select(kitten => ToKittenDto(kitten, litter.Id))], mother, father);
    }

    private static LitterDto ToLitterDto(LitterEntity litter, IReadOnlyList<KittenDto> kittensInLitter, CatDto? mother, CatDto? father)
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
            Mother = ToParentDto(mother),
            Father = ToParentDto(father),
        };
    }

    private static ParentDto? ToParentDto(CatDto? cat)
    {
        return cat == null ? null : new ParentDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Breed = cat.Breed,
            EmsCode = cat.EmsCode,
            Titles = cat.Titles,
            IsExternal = cat.Role == CatRole.External,
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
            Breed = kitten.Breed,
            EmsCode = kitten.EmsCode,
            Color = kitten.Color,
            Role = kitten.Role,
            Deceased = kitten.Deceased,
            DeathDate = kitten.DeathDate,
        };
    }
}
