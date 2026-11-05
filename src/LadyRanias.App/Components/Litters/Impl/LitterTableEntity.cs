using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed class LitterTableEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; } = ETag.All;

    public required string Name { get; set; }
    public required Guid MotherId { get; set; }
    public required Guid FatherId { get; set; }
    public required string Status { get; set; }       // enum som tekst, lesbart i Storage Explorer
    public DateTime? ExpectedDate { get; set; }       // Table Storage har ikke DateOnly
    public DateTime? BirthDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public required DateTimeOffset CreatedAt { get; set; }
}
