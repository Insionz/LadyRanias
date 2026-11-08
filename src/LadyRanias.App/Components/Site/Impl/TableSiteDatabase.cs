using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Site.Api;

namespace LadyRanias.App.Components.Site.Impl;

internal sealed class TableSiteDatabase : ISiteDatabase
{
    private const string Partition = "site";
    private const string Row = "settings";
    private readonly TableClient _tableClient;

    public TableSiteDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "site") { }

    internal TableSiteDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<SiteSettingsEntity?> GetAsync(CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<SiteSettingsTableEntity>(Partition, Row, cancellationToken: cancellationToken);
        if (!response.HasValue || response.Value == null) return null;

        var row = response.Value;
        return new SiteSettingsEntity
        {
            WelcomeText = row.WelcomeText,
            AboutText = row.AboutText,
            HeroMode = Enum.Parse<HeroMode>(row.HeroMode),
            HeroImageId = row.HeroImageId,
        };
    }

    public async Task SaveAsync(SiteSettingsEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(new SiteSettingsTableEntity
        {
            PartitionKey = Partition,
            RowKey = Row,
            ETag = ETag.All,
            WelcomeText = entity.WelcomeText,
            AboutText = entity.AboutText,
            HeroMode = entity.HeroMode.ToString(),
            HeroImageId = entity.HeroImageId,
        }, TableUpdateMode.Replace, cancellationToken);
    }
}
