using System.Collections.Concurrent;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed class InMemoryLitterDatabase : ILitterDatabase
{
    private readonly ConcurrentDictionary<Guid, LitterEntity> _storage = new();

    public Task<IReadOnlyList<LitterEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<LitterEntity>>([.. _storage.Values]);
    }

    public Task<LitterEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryGetValue(id, out var entity) ? entity : null);
    }

    public Task SaveAsync(LitterEntity entity, CancellationToken cancellationToken)
    {
        _storage[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryRemove(id, out _));
    }
}
