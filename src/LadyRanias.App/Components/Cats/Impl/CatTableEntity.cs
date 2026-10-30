using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Cats.Impl;

public record CatTableEntity : ITableEntity
{
    public required string RowKey { get; set; }
    public required string PartitionKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public required string Name { get; set; }
    public required string Sex { get; set; }
    public required DateTime BirthDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
    public ETag ETag { get; set; } = ETag.All;
}
