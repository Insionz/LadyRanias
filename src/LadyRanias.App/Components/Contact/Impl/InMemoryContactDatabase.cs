using System.Collections.Concurrent;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class InMemoryContactDatabase : IContactDatabase
{
    private readonly ConcurrentDictionary<Guid, ContactMessageEntity> _storage = new();

    public Task SaveAsync(ContactMessageEntity entity, CancellationToken cancellationToken)
    {
        _storage[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<ContactMessageEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryGetValue(id, out var entity) ? entity : null);
    }

    public Task<IReadOnlyList<ContactMessageEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ContactMessageEntity>>([.. _storage.Values]);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_storage.TryRemove(id, out _));
    }
}
