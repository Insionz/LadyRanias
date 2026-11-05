using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.Contact.Impl;

/// <summary>PartitionKey = "message" (alle i én partisjon), RowKey = meldingens id.</summary>
internal sealed class ContactTableEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; } = ETag.All;

    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }
    public required string Status { get; set; }   // enum som tekst, lesbart i Storage Explorer
    public DateTimeOffset? EmailSentAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
