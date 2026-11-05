using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed record LitterEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid MotherId { get; set; }
    public required Guid FatherId { get; set; }
    public required LitterStatus Status { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
}
