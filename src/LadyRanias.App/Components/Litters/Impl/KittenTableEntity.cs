using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Litters.Impl;

/// <summary>PartitionKey = kullets id, RowKey = kattungens id.</summary>
internal sealed class KittenTableEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; } = ETag.All;

    public required string Name { get; set; }
    public required string Sex { get; set; }
    public required string Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
}
