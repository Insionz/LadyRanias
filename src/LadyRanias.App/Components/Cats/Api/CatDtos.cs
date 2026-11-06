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

    /// <summary>Satt hvis katten er født her. Da er den (eller har vært) en kattunge i dette kullet.</summary>
    public Guid? LitterId { get; set; }

    /// <summary>Bare for kattunger: ledig, reservert, solgt eller beholdt.</summary>
    public KittenStatus? SaleStatus { get; set; }
}

public sealed record CreateCatDto
{
    [Required] public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required SexEnum Sex { get; set; }
    [Required] public required string Description { get; set; }
}

/// <summary>Full oppdatering: alle feltene sendes, også de som ikke er endret.</summary>
public sealed record UpdateCatDto
{
    [Required] public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required SexEnum Sex { get; set; }
    [Required] public required string Description { get; set; }
}

public enum SexEnum
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}

/// <summary>
/// Hvor en kattunge står i salget. Ligger i Cats fordi en kattunge er en katt.
/// Reglene for hvilke endringer som er lov, ligger i Litters (KittenStatusRules).
/// </summary>
public enum KittenStatus
{
    Available = 0,
    Reserved = 1,
    Sold = 2,
    Kept = 3,
}
