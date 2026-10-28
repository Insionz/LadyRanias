namespace LadyRanias.App.Components.Cats.Api;

public interface ICatHandler
{
    Task<IReadOnlyList<CatDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CatDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<CatDto> CreateAsync(CreateCatDto entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<CatDto?> SaveNameAsync(Guid id, ChangeNameDto name, CancellationToken cancellationToken);
}
