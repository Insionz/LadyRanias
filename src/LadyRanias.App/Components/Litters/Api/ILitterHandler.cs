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
    /// <summary>false hvis kattungen ikke finnes. InvalidStateException for en koblet voksen katt: den fjernes fra kullet i stedet.</summary>
    Task<bool> DeleteKittenAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken);

    /// <summary>
    /// Kobler en katt vi allerede har, til kullet den er født i (en beholdt kattunge som nå er voksen).
    /// null hvis kullet ikke finnes. ValidationException hvis katten ikke kan kobles. InvalidStateException hvis kullet ikke er født.
    /// </summary>
    Task<KittenDto?> LinkExistingCatAsync(Guid litterId, LinkCatDto link, CancellationToken cancellationToken);

    /// <summary>Fjerner en koblet voksen katt fra kullet. Katten finnes fortsatt. false hvis den ikke hører til kullet.</summary>
    Task<bool> UnlinkCatAsync(Guid litterId, Guid catId, CancellationToken cancellationToken);
}
