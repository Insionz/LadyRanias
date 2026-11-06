using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class CatImageTableEntity : ITableEntity
{
    public required string RowKey { get; set; }
    public required string PartitionKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public required string ContentType { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTime? TakenAt { get; set; }
    public ETag ETag { get; set; } = ETag.All;
}
