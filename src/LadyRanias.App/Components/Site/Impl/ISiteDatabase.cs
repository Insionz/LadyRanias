namespace LadyRanias.App.Components.Site.Impl;

internal interface ISiteDatabase
{
    /// <summary>null hvis innstillingene aldri er lagret.</summary>
    Task<SiteSettingsEntity?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(SiteSettingsEntity entity, CancellationToken cancellationToken);
}
