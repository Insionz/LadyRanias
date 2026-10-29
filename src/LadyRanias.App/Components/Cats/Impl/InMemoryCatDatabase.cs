using System.Collections.Concurrent;

namespace LadyRanias.App.Components.Cats.Impl;

internal sealed class InMemoryCatDatabase : ICatDatabase
{
    private readonly ConcurrentDictionary<Guid, CatEntity> _storage = new();
    
    public Task<IReadOnlyList<CatEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CatEntity>>(_storage.Values.OrderByDescending(o => o.CreatedAt).ToList());
    }

    public Task<CatEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryGetValue(id, out var entity) ? entity : null);
    }

    public Task SaveAsync(CatEntity entity, CancellationToken cancellationToken)
    {
        _storage[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.Remove(id, out var entity));
    }
}
