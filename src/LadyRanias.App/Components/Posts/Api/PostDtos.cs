using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Posts.Api;

/// <summary>Hva innlegget handler om. Brukes til filteret på nyhetssiden.</summary>
public enum PostKind
{
    Other = 0,
    Show = 1,      // utstilling
    NewCat = 2,    // ny katt i huset
    Title = 3,     // en katt har fått en tittel
    Litter = 4,    // kull og kattunger
}

public sealed record PostDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }

    /// <summary>Vanlig tekst. Tom linje = nytt avsnitt. Siden gjør lenker klikkbare selv.</summary>
    public required string Body { get; set; }

    /// <summary>Dagen det skjedde, ikke dagen innlegget ble skrevet. Innleggene sorteres etter denne.</summary>
    public required DateOnly Date { get; set; }
    public required PostKind Kind { get; set; }

    /// <summary>Kattene innlegget handler om. Innlegget vises også på sidene deres.</summary>
    public required IReadOnlyList<Guid> CatIds { get; set; }

    /// <summary>false = utkast. Bare admin ser det.</summary>
    public required bool Published { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Brukes både for nytt innlegg og for full oppdatering.</summary>
public sealed record SavePostDto
{
    [Required, StringLength(150)] public required string Title { get; set; }
    [StringLength(20000)] public string Body { get; set; } = string.Empty;
    public required DateOnly Date { get; set; }
    public PostKind Kind { get; set; } = PostKind.Other;
    public IReadOnlyList<Guid> CatIds { get; set; } = [];
    public bool Published { get; set; }
}
