namespace LadyRanias.App.Components.Litters.Impl;

internal interface ILitterDatabase
{
    Task<IReadOnlyList<LitterEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<LitterEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveAsync(LitterEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
