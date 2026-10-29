using LadyRanias.App.Components.Cats.Api;

namespace LadyRanias.App.Components.Cats.Impl;

internal sealed record CatEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required SexEnum Sex { get; set; }
    public required DateOnly BirthDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
}
