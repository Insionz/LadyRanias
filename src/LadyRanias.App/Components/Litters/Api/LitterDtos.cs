using LadyRanias.App.Components.Cats.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Litters.Api;

public enum LitterStatus
{
    Planned = 0,
    Born = 1,
}

public sealed record LitterDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid MotherId { get; set; }
    public required Guid FatherId { get; set; }
    public required LitterStatus Status { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>Kattungene følger med, så frontend får hele kullet i ett kall.</summary>
    public required IReadOnlyList<KittenDto> Kittens { get; set; }
}

/// <summary>
/// En kattunge sett fra kullet. Bak ligger en vanlig katt i Cats (samme Id), så bildene virker som for andre katter.
/// </summary>
public sealed record KittenDto
{
    public required Guid Id { get; set; }
    public required Guid LitterId { get; set; }
    public required string Name { get; set; }
    public required SexEnum Sex { get; set; }
    public required KittenStatus Status { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public sealed record CreateLitterDto
{
    [Required] public required string Name { get; set; }
    public required Guid MotherId { get; set; }
    public required Guid FatherId { get; set; }

    /// <summary>Valgfri. Ofte vet man bare omtrent når kullet kommer.</summary>
    public DateOnly? ExpectedDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed record MarkBornDto
{
    public required DateOnly BirthDate { get; set; }
}

public sealed record CreateKittenDto
{
    [Required] public required string Name { get; set; }
    public required SexEnum Sex { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed record ChangeKittenStatusDto
{
    public required KittenStatus Status { get; set; }
}
