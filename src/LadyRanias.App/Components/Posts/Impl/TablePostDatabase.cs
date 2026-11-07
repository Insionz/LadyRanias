using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Posts.Api;

namespace LadyRanias.App.Components.Posts.Impl;

internal sealed class TablePostDatabase : IPostDatabase
{
    private const string Partition = "post";
    private readonly TableClient _tableClient;

    public TablePostDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "posts") { }

    internal TablePostDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<IReadOnlyList<PostEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = _tableClient.QueryAsync<PostTableEntity>(e => e.PartitionKey == Partition, cancellationToken: cancellationToken);

        var all = new List<PostEntity>();
        await foreach (var row in query)
        {
            all.Add(ToEntity(row));
        }

        return all;
    }

    public async Task<PostEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<PostTableEntity>(Partition, id.ToString(), cancellationToken: cancellationToken);

        return response.HasValue && response.Value != null ? ToEntity(response.Value) : null;
    }

    public async Task SaveAsync(PostEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(ToRow(entity), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.DeleteEntityAsync(Partition, id.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;
    }

    private static PostTableEntity ToRow(PostEntity post) => new()
    {
        PartitionKey = Partition,
        RowKey = post.Id.ToString(),
        ETag = ETag.All,
        Title = post.Title,
        Body = post.Body,
        Date = post.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        Kind = post.Kind.ToString(),
        CatIds = string.Join(',', post.CatIds),
        Published = post.Published,
        CreatedAt = post.CreatedAt,
    };

    private static PostEntity ToEntity(PostTableEntity row) => new()
    {
        Id = Guid.Parse(row.RowKey),
        Title = row.Title,
        Body = row.Body,
        Date = DateOnly.FromDateTime(row.Date.ToUniversalTime()),
        Kind = Enum.Parse<PostKind>(row.Kind),
        CatIds = [.. row.CatIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse)],
        Published = row.Published,
        CreatedAt = row.CreatedAt,
    };
}
