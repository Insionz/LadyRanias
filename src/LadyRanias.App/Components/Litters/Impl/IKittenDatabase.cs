namespace LadyRanias.App.Components.Litters.Impl;

internal interface IKittenDatabase
{
    Task<IReadOnlyList<KittenEntity>> GetAllForLitterAsync(Guid litterId, CancellationToken cancellationToken);
    Task<KittenEntity?> GetAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken);
    Task SaveAsync(KittenEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken);
}
