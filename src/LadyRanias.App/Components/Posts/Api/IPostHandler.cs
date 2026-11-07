namespace LadyRanias.App.Components.Posts.Api;

/// <summary>
/// Nyheter og innlegg mamma skriver selv: utstillinger, nye katter, titler og kull.
/// null betyr «finnes ikke» (404). ValidationException betyr ugyldig input (400).
/// </summary>
public interface IPostHandler
{
    /// <summary>Publiserte innlegg, nyeste først. Med catId: bare de som handler om den katten.</summary>
    Task<IReadOnlyList<PostDto>> GetPublishedAsync(Guid? catId, CancellationToken cancellationToken);

    /// <summary>Alle innlegg, også utkast (admin).</summary>
    Task<IReadOnlyList<PostDto>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Et utkast finnes bare når includeDrafts er true.</summary>
    Task<PostDto?> GetAsync(Guid id, bool includeDrafts, CancellationToken cancellationToken);

    Task<PostDto> CreateAsync(SavePostDto post, CancellationToken cancellationToken);
    Task<PostDto?> UpdateAsync(Guid id, SavePostDto post, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
