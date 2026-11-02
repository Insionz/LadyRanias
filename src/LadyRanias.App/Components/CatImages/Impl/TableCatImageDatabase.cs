using Azure;
using Azure.Data.Tables;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class TableCatImageDatabase : ICatImageDatabase
{
    private readonly TableClient _tableClient;

    public TableCatImageDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "catimages") { }

    internal TableCatImageDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<IReadOnlyList<CatImageEntity>> GetAllForCatAsync(Guid catId, CancellationToken cancellationToken) 
    {
        var getAllImagesForCat = _tableClient.QueryAsync<CatImageTableEntity>(e => e.PartitionKey == catId.ToString(), cancellationToken: cancellationToken);

        List<CatImageEntity> allImagesOfCat = new();

        await foreach (var catImageEntity in getAllImagesForCat)
        {
            if (catImageEntity == null) continue;

            allImagesOfCat.Add(ToCatImageEntity(catImageEntity));
        }

        return [.. allImagesOfCat.OrderByDescending(e => e.CreatedAt)];
    }

    public async Task<CatImageEntity?> GetAsync(Guid catId, Guid imageId, CancellationToken cancellationToken) 
    {
        var response = await _tableClient.GetEntityIfExistsAsync<CatImageTableEntity>(catId.ToString(), imageId.ToString(), cancellationToken: cancellationToken);

        if (response.HasValue && response.Value != null)
            return ToCatImageEntity(response.Value);

        return null;
    }

    public async Task SaveAsync(CatImageEntity entity, CancellationToken cancellationToken) 
    {
        await _tableClient.UpsertEntityAsync(ToCatImageTableEntity(entity), mode: TableUpdateMode.Replace, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid catId, Guid imageId, CancellationToken cancellationToken) 
    {
        var response = await _tableClient.DeleteEntityAsync(catId.ToString(), imageId.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;        
    }

    private static CatImageTableEntity ToCatImageTableEntity(CatImageEntity catImage)
    {
        return new CatImageTableEntity
        {
            RowKey = catImage.ImageId.ToString(),
            PartitionKey = catImage.CatId.ToString(),
            ETag = ETag.All,
            ContentType = catImage.ContentType,
            CreatedAt = catImage.CreatedAt,
        };
    }

    private static CatImageEntity ToCatImageEntity(CatImageTableEntity catImage)
    {
        return new CatImageEntity
        {
            CatId = Guid.Parse(catImage.PartitionKey),
            ImageId = Guid.Parse(catImage.RowKey),
            ContentType = catImage.ContentType,
            CreatedAt = catImage.CreatedAt,
        };
    }
}
