using LadyRanias.App.Components.Site.Api;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Site.Impl;

internal sealed class SiteHandler(ISiteDatabase database) : ISiteHandler
{
    /// <summary>Teksten som stod fast i koden før mamma kunne endre den.</summary>
    internal const string DefaultWelcomeText =
        "Lady Rania’s er et lite oppdrett av Maine Coon og Hellig Birma. Kattene våre vokser opp hjemme, med mennesker rundt seg hver dag.";

    public async Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await database.GetAsync(cancellationToken);

        return settings == null
            ? new SiteSettingsDto { WelcomeText = DefaultWelcomeText, AboutText = string.Empty, HeroMode = HeroMode.Newest }
            : ToDto(settings);
    }

    public async Task<SiteSettingsDto> SaveAsync(SaveSiteSettingsDto settings, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(settings.HeroMode))
        {
            throw new ValidationException("HeroMode is not valid");
        }

        if (settings.HeroMode == HeroMode.Fixed && settings.HeroImageId == null)
        {
            throw new ValidationException("HeroImageId: choose the image to show");
        }

        var entity = new SiteSettingsEntity
        {
            WelcomeText = (settings.WelcomeText ?? string.Empty).Trim(),
            AboutText = (settings.AboutText ?? string.Empty).Trim(),
            HeroMode = settings.HeroMode,
            // Bildet huskes bare når det brukes, så et gammelt valg ikke dukker opp igjen senere.
            HeroImageId = settings.HeroMode == HeroMode.Fixed ? settings.HeroImageId : null,
        };

        await database.SaveAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    private static SiteSettingsDto ToDto(SiteSettingsEntity settings) => new()
    {
        WelcomeText = settings.WelcomeText,
        AboutText = settings.AboutText,
        HeroMode = settings.HeroMode,
        HeroImageId = settings.HeroImageId,
    };
}
