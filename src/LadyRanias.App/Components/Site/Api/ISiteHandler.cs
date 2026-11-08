namespace LadyRanias.App.Components.Site.Api;

public interface ISiteHandler
{
    /// <summary>Innstillingene. Er de aldri lagret, kommer standardtekstene, så forsiden ser ut som før.</summary>
    Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken);

    /// <summary>ValidationException ved ugyldige verdier.</summary>
    Task<SiteSettingsDto> SaveAsync(SaveSiteSettingsDto settings, CancellationToken cancellationToken);
}
