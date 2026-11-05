using System.Collections.Concurrent;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed class InMemoryKittenDatabase : IKittenDatabase
{
    // Nøkkelen er (kull, kattunge), som PartitionKey og RowKey i Table Storage.
    private readonly ConcurrentDictionary<(Guid LitterId, Guid KittenId), KittenEntity> _storage = new();

    public Task<IReadOnlyList<KittenEntity>> GetAllForLitterAsync(Guid litterId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<KittenEntity>>([.. _storage.Values.Where(k => k.LitterId == litterId)]);
    }

    public Task<KittenEntity?> GetAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryGetValue((litterId, kittenId), out var entity) ? entity : null);
    }

    public Task SaveAsync(KittenEntity entity, CancellationToken cancellationToken)
    {
        _storage[(entity.LitterId, entity.Id)] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryRemove((litterId, kittenId), out _));
    }
}
