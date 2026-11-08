using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Site.Impl;

/// <summary>Bare én rad: PartitionKey «site», RowKey «settings».</summary>
internal sealed class SiteSettingsTableEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; } = ETag.All;

    public string WelcomeText { get; set; } = string.Empty;
    public string AboutText { get; set; } = string.Empty;
    public string HeroMode { get; set; } = "Newest";
    public Guid? HeroImageId { get; set; }
}
