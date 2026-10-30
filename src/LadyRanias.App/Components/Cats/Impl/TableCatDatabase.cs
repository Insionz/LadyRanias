using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Cats.Api;

namespace LadyRanias.App.Components.Cats.Impl;

internal sealed class TableCatDatabase : ICatDatabase
{
    private readonly TableClient _tableClient;

    public TableCatDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "cats") { }
    
    internal TableCatDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<IReadOnlyList<CatEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        var getAll = _tableClient.QueryAsync<CatTableEntity>(e => e.PartitionKey == "cat", cancellationToken: cancellationToken);

        List<CatEntity> allCats = new();

        await foreach (var cat in getAll)
        {
            if (cat == null) continue;

            allCats.Add(ToCatEntity(cat));
        }

        return [.. allCats.OrderByDescending(o => o.CreatedAt)];
    }

    public async Task<CatEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<CatTableEntity>("cat", id.ToString(), cancellationToken: cancellationToken);

        if (response.HasValue && response.Value != null)
            return ToCatEntity(response.Value);

        return null;
    }

    public async Task SaveAsync(CatEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(ToCatTableEntity(entity), mode: TableUpdateMode.Replace, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.DeleteEntityAsync("cat", id.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;
    }

    private static CatTableEntity ToCatTableEntity(CatEntity entity)
    {
        return new CatTableEntity
        {
            RowKey = entity.Id.ToString(),
            PartitionKey = "cat",
            ETag = ETag.All,
            Name = entity.Name,
            Sex = entity.Sex.ToString(),
            BirthDate = entity.BirthDate.ToDateTime(new TimeOnly(), DateTimeKind.Utc),
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
        };
    }

    private static CatEntity ToCatEntity(CatTableEntity cat)
    {
        return new CatEntity
        {
            Id = Guid.Parse(cat.RowKey),
            Name = cat.Name,
            BirthDate = DateOnly.FromDateTime(cat.BirthDate),
            Description = cat.Description,
            Sex = Enum.Parse<SexEnum>(cat.Sex),
            CreatedAt = cat.CreatedAt,
        };
    }
}
