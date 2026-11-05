using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Litters.Impl;

// Litters bruker ICatHandler fra Cats/Api for å slå opp mor og far.
// Avhengigheten går bare én vei: Cats vet ingenting om Litters.
internal sealed class LitterHandler(
    ILitterDatabase litters,
    IKittenDatabase kittens,
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
        var kittensInLitter = await kittens.GetAllForLitterAsync(id, cancellationToken);

        var deleted = await litters.DeleteAsync(id, cancellationToken);
        if (!deleted) return false;

        // Samme rekkefølge som når en katt slettes: hovedraden først, så oppryddingen.
        // CancellationToken.None: oppryddingen skal fullføres selv om forespørselen avbrytes.
        foreach (var kitten in kittensInLitter)
        {
            await kittens.DeleteAsync(id, kitten.Id, CancellationToken.None);
        }

        return true;
    }

    public async Task<KittenDto?> AddKittenAsync(Guid litterId, CreateKittenDto newKitten, CancellationToken cancellationToken)
    {
        var litter = await litters.GetAsync(litterId, cancellationToken);
        if (litter == null) return null;

        if (litter.Status != LitterStatus.Born)
        {
            throw new InvalidStateException("Kittens can only be added to a litter that is born");
        }

        if (newKitten.Sex == SexEnum.Unknown)
        {
            throw new ValidationException("Sex must be either male or female");
        }

        var kitten = new KittenEntity
        {
            Id = Guid.NewGuid(),
            LitterId = litterId,
            Name = newKitten.Name,
            Sex = newKitten.Sex,
            Status = KittenStatus.Available,
            Description = newKitten.Description,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await kittens.SaveAsync(kitten, cancellationToken);

        return ToKittenDto(kitten);
    }

    public async Task<KittenDto?> ChangeKittenStatusAsync(Guid litterId, Guid kittenId, ChangeKittenStatusDto change, CancellationToken cancellationToken)
    {
        var kitten = await kittens.GetAsync(litterId, kittenId, cancellationToken);
        if (kitten == null) return null;

        if (!KittenStatusRules.CanChange(kitten.Status, change.Status))
        {
            throw new InvalidStateException($"Cannot change kitten status from {kitten.Status} to {change.Status}");
        }

        var changed = kitten with { Status = change.Status };
        await kittens.SaveAsync(changed, cancellationToken);

        return ToKittenDto(changed);
    }

    public Task<bool> DeleteKittenAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        return kittens.DeleteAsync(litterId, kittenId, cancellationToken);
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
    }

    private async Task<LitterDto> ToDtoWithKittensAsync(LitterEntity litter, CancellationToken cancellationToken)
    {
        var kittensInLitter = await kittens.GetAllForLitterAsync(litter.Id, cancellationToken);

        return ToLitterDto(litter, [.. kittensInLitter.OrderBy(k => k.CreatedAt).Select(ToKittenDto)]);
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

    private static KittenDto ToKittenDto(KittenEntity kitten)
    {
        return new KittenDto
        {
            Id = kitten.Id,
            LitterId = kitten.LitterId,
            Name = kitten.Name,
            Sex = kitten.Sex,
            Status = kitten.Status,
            Description = kitten.Description,
            CreatedAt = kitten.CreatedAt,
        };
    }
}
