using LadyRanias.App.Components.Site.Api;

namespace LadyRanias.App.Components.Site.Impl;

internal sealed record SiteSettingsEntity
{
    public required string WelcomeText { get; set; }
    public required string AboutText { get; set; }
    public required HeroMode HeroMode { get; set; }
    public Guid? HeroImageId { get; set; }
}
