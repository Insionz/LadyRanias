using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed class TableKittenDatabase : IKittenDatabase
{
    private readonly TableClient _tableClient;

    public TableKittenDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "kittens") { }

    internal TableKittenDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<IReadOnlyList<KittenEntity>> GetAllForLitterAsync(Guid litterId, CancellationToken cancellationToken)
    {
        // Alle kattunger i ett kull ligger i samme partisjon, så dette er ett raskt oppslag.
        var partition = litterId.ToString();
        var query = _tableClient.QueryAsync<KittenTableEntity>(e => e.PartitionKey == partition, cancellationToken: cancellationToken);

        var all = new List<KittenEntity>();
        await foreach (var row in query)
        {
            all.Add(ToEntity(row));
        }

        return all;
    }

    public async Task<KittenEntity?> GetAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<KittenTableEntity>(litterId.ToString(), kittenId.ToString(), cancellationToken: cancellationToken);

        return response.HasValue && response.Value != null ? ToEntity(response.Value) : null;
    }

    public async Task SaveAsync(KittenEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(ToRow(entity), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid litterId, Guid kittenId, CancellationToken cancellationToken)
    {
        var response = await _tableClient.DeleteEntityAsync(litterId.ToString(), kittenId.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;
    }

    private static KittenTableEntity ToRow(KittenEntity kitten) => new()
    {
        PartitionKey = kitten.LitterId.ToString(),
        RowKey = kitten.Id.ToString(),
        ETag = ETag.All,
        Name = kitten.Name,
        Sex = kitten.Sex.ToString(),
        Status = kitten.Status.ToString(),
        Description = kitten.Description,
        CreatedAt = kitten.CreatedAt,
    };

    private static KittenEntity ToEntity(KittenTableEntity row) => new()
    {
        Id = Guid.Parse(row.RowKey),
        LitterId = Guid.Parse(row.PartitionKey),
        Name = row.Name,
        Sex = Enum.Parse<SexEnum>(row.Sex),
        Status = Enum.Parse<KittenStatus>(row.Status),
        Description = row.Description,
        CreatedAt = row.CreatedAt,
    };
}
