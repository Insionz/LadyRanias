using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Contact.Api;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class TableContactDatabase : IContactDatabase
{
    // Én partisjon, som for katter. Det er noen titalls meldinger i året, langt under hva én partisjon tåler.
    // Da blir GetAsync(id) et direkte oppslag, uten at vi må vite noe annet enn id-en.
    private const string Partition = "message";
    private readonly TableClient _tableClient;

    public TableContactDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "contactmessages") { }

    internal TableContactDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task SaveAsync(ContactMessageEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(ToRow(entity), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<ContactMessageEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<ContactTableEntity>(Partition, id.ToString(), cancellationToken: cancellationToken);

        return response.HasValue && response.Value != null ? ToEntity(response.Value) : null;
    }

    public async Task<IReadOnlyList<ContactMessageEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = _tableClient.QueryAsync<ContactTableEntity>(e => e.PartitionKey == Partition, cancellationToken: cancellationToken);

        var all = new List<ContactMessageEntity>();
        await foreach (var row in query)
        {
            all.Add(ToEntity(row));
        }

        return [.. all.OrderByDescending(m => m.CreatedAt)];
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.DeleteEntityAsync(Partition, id.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;
    }

    private static ContactTableEntity ToRow(ContactMessageEntity message) => new()
    {
        PartitionKey = Partition,
        RowKey = message.Id.ToString(),
        ETag = ETag.All,
        Name = message.Name,
        Email = message.Email,
        Phone = message.Phone,
        Subject = message.Subject,
        Message = message.Message,
        Status = message.Status.ToString(),
        EmailSentAt = message.EmailSentAt,
        CreatedAt = message.CreatedAt,
    };

    private static ContactMessageEntity ToEntity(ContactTableEntity row) => new()
    {
        Id = Guid.Parse(row.RowKey),
        Name = row.Name,
        Email = row.Email,
        Phone = row.Phone,
        Subject = row.Subject,
        Message = row.Message,
        Status = Enum.Parse<ContactStatusEnum>(row.Status),
        EmailSentAt = row.EmailSentAt,
        CreatedAt = row.CreatedAt,
    };
}
