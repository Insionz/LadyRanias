using LadyRanias.App.Components.Cats.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Cats.Impl;

internal sealed class CatHandler(ICatDatabase database, TimeProvider timeProvider) : ICatHandler
{
    public async Task<IReadOnlyList<CatDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var allCats = await database.GetAllAsync(cancellationToken);

        return allCats.Select(ToDto).ToList();
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
    public async Task<CatDto?> SaveNameAsync(Guid id, ChangeNameDto changeNameDto, CancellationToken cancellationToken)
    {
        var cat = await database.GetAsync(id, cancellationToken);

        if (cat == null)
        {
            return null;
        }

        var changedCat = cat with
        {
            Name = changeNameDto.Name,
        };

        await database.SaveAsync(changedCat, cancellationToken);
        return ToDto(changedCat);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await database.DeleteAsync(id, cancellationToken);
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
