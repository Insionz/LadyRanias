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

    // Nullbare, så katter som ble lagret før steg 14 (uten disse kolonnene) kan leses som før.
    public Guid? LitterId { get; set; }
    public string? SaleStatus { get; set; }
    public string? Breed { get; set; }
    public string? Role { get; set; }
    public string? EmsCode { get; set; }
    public string? Color { get; set; }
    public bool? Neutered { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Titles { get; set; }
    public DateTime? DeathDate { get; set; }
    public ETag ETag { get; set; } = ETag.All;
}
