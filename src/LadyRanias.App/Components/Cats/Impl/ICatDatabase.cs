namespace LadyRanias.App.Components.Cats.Impl;

internal interface ICatDatabase
{
    Task<IReadOnlyList<CatEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<CatEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveAsync(CatEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
