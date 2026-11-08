using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Posts.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Posts.Impl;

// Posts bruker ICatHandler for å sjekke at kattene i et innlegg finnes. Cats vet ingenting om Posts.
internal sealed class PostHandler(
    IPostDatabase database,
    ICatHandler cats,
    TimeProvider timeProvider,
    IEnumerable<IPostDeletedListener> deletedListeners) : IPostHandler
{
    public async Task<IReadOnlyList<PostDto>> GetPublishedAsync(Guid? catId, CancellationToken cancellationToken)
    {
        var all = await database.GetAllAsync(cancellationToken);

        return Sorted(all.Where(post => post.Published && (catId == null || post.CatIds.Contains(catId.Value))));
    }

    public async Task<IReadOnlyList<PostDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Sorted(await database.GetAllAsync(cancellationToken));
    }

    public async Task<PostDto?> GetAsync(Guid id, bool includeDrafts, CancellationToken cancellationToken)
    {
        var post = await database.GetAsync(id, cancellationToken);

        return post == null || (!post.Published && !includeDrafts) ? null : ToDto(post);
    }

    public async Task<PostDto> CreateAsync(SavePostDto post, CancellationToken cancellationToken)
    {
        var catIds = await ValidateAsync(post, cancellationToken);

        var entity = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = post.Title.Trim(),
            Body = (post.Body ?? string.Empty).Trim(),
            Date = post.Date,
            Kind = post.Kind,
            CatIds = catIds,
            Published = post.Published,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await database.SaveAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async Task<PostDto?> UpdateAsync(Guid id, SavePostDto post, CancellationToken cancellationToken)
    {
        var existing = await database.GetAsync(id, cancellationToken);
        if (existing == null) return null;

        var catIds = await ValidateAsync(post, cancellationToken);

        var updated = existing with
        {
            Title = post.Title.Trim(),
            Body = (post.Body ?? string.Empty).Trim(),
            Date = post.Date,
            Kind = post.Kind,
            CatIds = catIds,
            Published = post.Published,
        };

        await database.SaveAsync(updated, cancellationToken);
        return ToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await database.DeleteAsync(id, cancellationToken);
        if (!deleted) return false;

        // Bildene til innlegget (CatImages). Samme mønster som for katter og kull.
        foreach (var listener in deletedListeners)
        {
            await listener.OnPostDeletedAsync(id, CancellationToken.None);
        }

        return true;
    }

    /// <summary>Tittelen må ha tekst, og kattene må finnes. Gir tilbake kattene uten duplikater.</summary>
    private async Task<IReadOnlyList<Guid>> ValidateAsync(SavePostDto post, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(post.Title))
        {
            throw new ValidationException("Title must be set");
        }

        if (!Enum.IsDefined(post.Kind))
        {
            throw new ValidationException("Kind is not valid");
        }

        var catIds = (post.CatIds ?? Array.Empty<Guid>()).Distinct().ToList();
        foreach (var catId in catIds)
        {
            if (await cats.GetAsync(catId, cancellationToken) == null)
            {
                throw new ValidationException($"CatIds: no cat with id {catId}");
            }
        }

        return catIds;
    }

    /// <summary>Nyeste hendelse først. Samme dag: sist skrevet først.</summary>
    private static List<PostDto> Sorted(IEnumerable<PostEntity> posts) =>
        [.. posts.OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt).Select(ToDto)];

    private static PostDto ToDto(PostEntity post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Body = post.Body,
        Date = post.Date,
        Kind = post.Kind,
        CatIds = post.CatIds,
        Published = post.Published,
        CreatedAt = post.CreatedAt,
    };
}
