using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed record KittenEntity
{
    public required Guid Id { get; set; }
    public required Guid LitterId { get; set; }
    public required string Name { get; set; }
    public required SexEnum Sex { get; set; }
    public required KittenStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
}
