namespace LadyRanias.App.Components.CatImages.Impl;

internal interface ICatImageDatabase
{
    Task<IReadOnlyList<CatImageEntity>> GetAllForCatAsync(Guid catId, CancellationToken cancellationToken);
    Task<CatImageEntity?> GetAsync(Guid catId, Guid imageId, CancellationToken cancellationToken);
    Task SaveAsync(CatImageEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken);
}
