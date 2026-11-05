namespace LadyRanias.App.Components.Litters.Api;

/// <summary>
/// null betyr «finnes ikke» (404).
/// ValidationException betyr ugyldig input (400).
/// InvalidStateException betyr at handlingen ikke passer med tilstanden (409).
/// </summary>
public interface ILitterHandler
{
    Task<IReadOnlyList<LitterDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<LitterDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<LitterDto> CreateAsync(CreateLitterDto newLitter, CancellationToken cancellationToken);
    Task<LitterDto?> MarkBornAsync(Guid id, MarkBornDto born, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<KittenDto?> AddKittenAsync(Guid litterId, CreateKittenDto newKitten, CancellationToken cancellationToken);
    Task<KittenDto?> ChangeKittenStatusAsync(Guid litterId, Guid kittenId, ChangeKittenStatusDto change, CancellationToken cancellationToken);
    Task<bool> DeleteKittenAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken);
}
