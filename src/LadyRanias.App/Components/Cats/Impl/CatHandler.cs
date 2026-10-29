using LadyRanias.App.Components.Cats.Api;

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
    public async Task<CatDto> CreateAsync(CreateCatDto entity, CancellationToken cancellationToken)
    {
        var cat = new CatEntity
        {
            Id = Guid.NewGuid(),
            Name = entity.Name,
            Sex = entity.Sex,
            Description = entity.Description,
            BirthDate = entity.BirthDate,
            CreatedAt = timeProvider.GetUtcNow(),
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

    private static CatDto ToDto(CatEntity entity)
    {
        return new CatDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Sex = entity.Sex,
            Description = entity.Description,
            BirthDate = entity.BirthDate,
            CreatedAt = entity.CreatedAt,
        };
    }
}
