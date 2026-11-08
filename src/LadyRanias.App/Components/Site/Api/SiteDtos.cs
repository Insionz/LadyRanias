using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Site.Api;

/// <summary>Hvilket bilde forsiden viser.</summary>
public enum HeroMode
{
    /// <summary>Det nyeste bildet av den nyeste katten. Slik det var før mamma kunne velge.</summary>
    Newest = 0,

    /// <summary>Ett bilde mamma har valgt (HeroImageId).</summary>
    Fixed = 1,

    /// <summary>Et tilfeldig av forsidebildene hver gang siden åpnes.</summary>
    Random = 2,

    /// <summary>Forsidebildene glir over i hverandre mens man ser på siden.</summary>
    Slideshow = 3,
}

/// <summary>Tekstene og forsidebildet mamma styrer selv.</summary>
public sealed record SiteSettingsDto
{
    /// <summary>Den korte teksten på forsiden, under «Velkommen».</summary>
    public required string WelcomeText { get; set; }

    /// <summary>Siden «Om oss». Vanlig tekst: tom linje = nytt avsnitt, lenker blir klikkbare.</summary>
    public required string AboutText { get; set; }

    public required HeroMode HeroMode { get; set; }

    /// <summary>Bildet som vises når HeroMode er Fixed. Er bildet slettet, viser siden det første forsidebildet.</summary>
    public Guid? HeroImageId { get; set; }
}

public sealed record SaveSiteSettingsDto
{
    [StringLength(2000)] public string WelcomeText { get; set; } = string.Empty;
    [StringLength(30000)] public string AboutText { get; set; } = string.Empty;
    public HeroMode HeroMode { get; set; } = HeroMode.Newest;
    public Guid? HeroImageId { get; set; }
}
