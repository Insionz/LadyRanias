namespace LadyRanias.App.Components.Posts.Impl;

internal interface IPostDatabase
{
    Task<IReadOnlyList<PostEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<PostEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveAsync(PostEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
