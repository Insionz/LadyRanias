using System.Collections.Concurrent;

namespace LadyRanias.App.Components.Posts.Impl;

internal sealed class InMemoryPostDatabase : IPostDatabase
{
    private readonly ConcurrentDictionary<Guid, PostEntity> _storage = new();

    public Task<IReadOnlyList<PostEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PostEntity>>([.. _storage.Values]);

    public Task<PostEntity?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_storage.TryGetValue(id, out var entity) ? entity : null);

    public Task SaveAsync(PostEntity entity, CancellationToken cancellationToken)
    {
        _storage[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_storage.TryRemove(id, out _));
}
