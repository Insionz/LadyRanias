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
    public Breed Breed { get; set; } = Breed.Unknown;
    public CatRole Role { get; set; } = CatRole.Breeding;
    public string? EmsCode { get; set; }
    public string? Color { get; set; }
    public bool Neutered { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Titles { get; set; }
    public bool Deceased { get; set; }
    public DateOnly? DeathDate { get; set; }
    public Guid? LitterId { get; set; }
    public KittenStatus? SaleStatus { get; set; }
}
