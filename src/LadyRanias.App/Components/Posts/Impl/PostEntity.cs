using LadyRanias.App.Components.Posts.Api;

namespace LadyRanias.App.Components.Posts.Impl;

internal sealed record PostEntity
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = string.Empty;
    public required DateOnly Date { get; set; }
    public required PostKind Kind { get; set; }
    public IReadOnlyList<Guid> CatIds { get; set; } = [];
    public required bool Published { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    // Records sammenligner lister på referanse. Testene trenger at to innlegg med de samme kattene er like.
    public bool Equals(PostEntity? other) =>
        other is not null
        && Id == other.Id && Title == other.Title && Body == other.Body && Date == other.Date && Kind == other.Kind
        && Published == other.Published && CreatedAt == other.CreatedAt && CatIds.SequenceEqual(other.CatIds);

    public override int GetHashCode() => Id.GetHashCode();
}
