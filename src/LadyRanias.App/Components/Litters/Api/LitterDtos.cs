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

    /// <summary>
    /// Navn og rase på mor og far, så siden kan vise dem uten å hente kattene selv.
    /// Det trengs fordi en ekstern far ikke er med i lista over våre katter. null hvis katten er slettet.
    /// </summary>
    public ParentDto? Mother { get; set; }
    public ParentDto? Father { get; set; }
}

public sealed record ParentDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Breed Breed { get; set; }
    public string? EmsCode { get; set; }
    public string? Titles { get; set; }

    /// <summary>Fra et annet oppdrett. Siden lenker ikke til den, for den hører ikke til våre katter.</summary>
    public required bool IsExternal { get; set; }
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
    public Breed Breed { get; set; }
    public string? EmsCode { get; set; }
    public string? Color { get; set; }

    /// <summary>En kattunge som ikke klarte seg. Den offentlige kullsiden viser den ikke.</summary>
    public bool Deceased { get; set; }
    public DateOnly? DeathDate { get; set; }
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
