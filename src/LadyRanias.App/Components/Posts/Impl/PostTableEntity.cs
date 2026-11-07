using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Posts.Impl;

internal sealed class PostTableEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; } = ETag.All;

    public required string Title { get; set; }

    /// <summary>En tekstkolonne i Table Storage kan være opptil 64 KB, så 20 000 tegn er godt innenfor.</summary>
    public string Body { get; set; } = string.Empty;
    public required DateTime Date { get; set; }
    public required string Kind { get; set; }

    /// <summary>Table Storage har ikke lister. Id-ene lagres som tekst, skilt med komma.</summary>
    public string CatIds { get; set; } = string.Empty;
    public required bool Published { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
