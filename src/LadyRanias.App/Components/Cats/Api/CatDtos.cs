using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Cats.Api;

public sealed record CatDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required SexEnum Sex { get; set; }

    public required DateOnly BirthDate { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public sealed record CreateCatDto
{
    [Required] public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required SexEnum Sex { get; set; }
    [Required] public required string Description { get; set; }
}

public sealed record ChangeNameDto
{
    public required string Name { get; set; }
}

public enum SexEnum
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}
