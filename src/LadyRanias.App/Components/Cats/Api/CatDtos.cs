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

    /// <summary>Unknown for katter som ble lagt inn før rasen fantes. Mamma må sette den i admin.</summary>
    public required Breed Breed { get; set; }
    public required CatRole Role { get; set; }
    public string? EmsCode { get; set; }
    public string? Color { get; set; }
    public bool Neutered { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Titles { get; set; }

    /// <summary>
    /// En død katt slettes ikke: den er fortsatt mor eller far i kullene sine, og vises på minnesiden.
    /// Datoen er valgfri, for for mange av de gamle kattene husker man den ikke.
    /// </summary>
    public bool Deceased { get; set; }
    public DateOnly? DeathDate { get; set; }

    /// <summary>
    /// Mor og far for en katt som ikke er født her (kjøpt fra et annet oppdrett).
    /// Enten en katt i systemet (Id, også en ekstern), eller bare et navn. For katter født her kommer de fra kullet.
    /// </summary>
    public Guid? MotherId { get; set; }
    public string? MotherName { get; set; }
    public Guid? FatherId { get; set; }
    public string? FatherName { get; set; }

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
    /// <summary>Valgfri. Tom tekst er greit.</summary>
    public string Description { get; set; } = string.Empty;
    public required Breed Breed { get; set; }

    /// <summary>Breeding, Retired eller External. Kattunger lages bare via kullet.</summary>
    public CatRole Role { get; set; } = CatRole.Breeding;

    /// <summary>«MCO n 22». Valgfri. Er den fylt ut, må den begynne med rasekoden.</summary>
    [StringLength(30)] public string? EmsCode { get; set; }
    [StringLength(60)] public string? Color { get; set; }
    public bool Neutered { get; set; }
    [StringLength(60)] public string? RegistrationNumber { get; set; }
    [StringLength(60)] public string? Titles { get; set; }

    /// <summary>Død, med eller uten dato. En dødsdato betyr at katten er død, også om Deceased ikke er satt.</summary>
    public bool Deceased { get; set; }

    /// <summary>Valgfri. Ikke før fødselsdatoen, og ikke i framtiden.</summary>
    public DateOnly? DeathDate { get; set; }

    /// <summary>En katt i systemet, eller et navn hvis forelderen aldri har vært her. Id vinner hvis begge er satt.</summary>
    public Guid? MotherId { get; set; }
    [StringLength(120)] public string? MotherName { get; set; }
    public Guid? FatherId { get; set; }
    [StringLength(120)] public string? FatherName { get; set; }
}

/// <summary>Full oppdatering: alle feltene sendes, også de som ikke er endret.</summary>
public sealed record UpdateCatDto
{
    [Required] public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required SexEnum Sex { get; set; }
    /// <summary>Valgfri. Tom tekst er greit.</summary>
    public string Description { get; set; } = string.Empty;
    public required Breed Breed { get; set; }

    /// <summary>En beholdt kattunge kan bli Breeding eller Retired. Bare katter født i et kull kan være Kitten.</summary>
    public CatRole Role { get; set; } = CatRole.Breeding;

    /// <summary>«MCO n 22». Valgfri. Er den fylt ut, må den begynne med rasekoden.</summary>
    [StringLength(30)] public string? EmsCode { get; set; }
    [StringLength(60)] public string? Color { get; set; }
    public bool Neutered { get; set; }
    [StringLength(60)] public string? RegistrationNumber { get; set; }
    [StringLength(60)] public string? Titles { get; set; }

    /// <summary>Død, med eller uten dato. En dødsdato betyr at katten er død, også om Deceased ikke er satt.</summary>
    public bool Deceased { get; set; }

    /// <summary>Valgfri. Ikke før fødselsdatoen, og ikke i framtiden.</summary>
    public DateOnly? DeathDate { get; set; }

    /// <summary>En katt i systemet, eller et navn hvis forelderen aldri har vært her. Id vinner hvis begge er satt.</summary>
    public Guid? MotherId { get; set; }
    [StringLength(120)] public string? MotherName { get; set; }
    public Guid? FatherId { get; set; }
    [StringLength(120)] public string? FatherName { get; set; }
}

public enum SexEnum
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}

/// <summary>EMS-koden for rasen: MCO og SBI. Unknown avvises ved lagring, som SexEnum.Unknown.</summary>
public enum Breed
{
    Unknown = 0,
    MaineCoon = 1,
    SacredBirman = 2,
}

public enum CatRole
{
    /// <summary>I avl hos oss</summary>
    Breeding = 0,

    /// <summary>Pensjonert, bor fortsatt her</summary>
    Retired = 1,

    /// <summary>Født i et kull hos oss. Har LitterId og SaleStatus.</summary>
    Kitten = 2,

    /// <summary>Fra et annet oppdrett, for eksempel en lånt hannkatt. Kan være forelder, men vises ikke blant våre katter.</summary>
    External = 3,
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

    /// <summary>Oppdretteren har ikke bestemt seg: beholdes den, eller skal den selges? Ikke ledig ennå.</summary>
    UnderEvaluation = 4,
}
