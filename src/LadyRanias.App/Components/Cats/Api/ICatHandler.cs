namespace LadyRanias.App.Components.Cats.Api;

public interface ICatHandler
{
    Task<IReadOnlyList<CatDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CatDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CatDto> CreateAsync(CreateCatDto entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>null hvis katten ikke finnes. ValidationException ved ugyldige verdier.</summary>
    Task<CatDto?> UpdateAsync(Guid id, UpdateCatDto update, CancellationToken cancellationToken);
}
