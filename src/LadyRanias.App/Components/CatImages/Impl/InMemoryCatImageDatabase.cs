using System.Collections.Concurrent;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class InMemoryCatImageDatabase : ICatImageDatabase
{
    private readonly ConcurrentDictionary<(Guid CatId, Guid ImageId), CatImageEntity> _storage = new();

    public Task<IReadOnlyList<CatImageEntity>> GetAllForCatAsync(Guid catId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CatImageEntity>>([.. _storage.Values.Where(e => e.CatId == catId)]);
    }

    public Task<CatImageEntity?> GetAsync(Guid catId, Guid imageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryGetValue((catId, imageId), out var image) ? image : null);
    }

    public Task SaveAsync(CatImageEntity entity, CancellationToken cancellationToken)
    {
        _storage[(entity.CatId, entity.ImageId)] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryRemove((catId, imageId), out _));
    }
}
