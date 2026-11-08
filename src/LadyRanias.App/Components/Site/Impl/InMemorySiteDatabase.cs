namespace LadyRanias.App.Components.Site.Impl;

internal sealed class InMemorySiteDatabase : ISiteDatabase
{
    private SiteSettingsEntity? _settings;

    public Task<SiteSettingsEntity?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_settings);

    public Task SaveAsync(SiteSettingsEntity entity, CancellationToken cancellationToken)
    {
        _settings = entity;
        return Task.CompletedTask;
    }
}
