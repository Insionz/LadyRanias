using Azure;
using Azure.Data.Tables;
using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.Litters.Impl;

internal sealed class TableLitterDatabase : ILitterDatabase
{
    private const string Partition = "litter";
    private readonly TableClient _tableClient;

    public TableLitterDatabase(TableServiceClient tableServiceClient) : this(tableServiceClient, "litters") { }

    internal TableLitterDatabase(TableServiceClient tableServiceClient, string tableName)
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<IReadOnlyList<LitterEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = _tableClient.QueryAsync<LitterTableEntity>(e => e.PartitionKey == Partition, cancellationToken: cancellationToken);

        var all = new List<LitterEntity>();
        await foreach (var row in query)
        {
            all.Add(ToEntity(row));
        }

        return all;
    }

    public async Task<LitterEntity?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.GetEntityIfExistsAsync<LitterTableEntity>(Partition, id.ToString(), cancellationToken: cancellationToken);

        return response.HasValue && response.Value != null ? ToEntity(response.Value) : null;
    }

    public async Task SaveAsync(LitterEntity entity, CancellationToken cancellationToken)
    {
        await _tableClient.UpsertEntityAsync(ToRow(entity), TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _tableClient.DeleteEntityAsync(Partition, id.ToString(), cancellationToken: cancellationToken);

        return !response.IsError;
    }

    private static LitterTableEntity ToRow(LitterEntity litter) => new()
    {
        PartitionKey = Partition,
        RowKey = litter.Id.ToString(),
        ETag = ETag.All,
        Name = litter.Name,
        MotherId = litter.MotherId,
        FatherId = litter.FatherId,
        Status = litter.Status.ToString(),
        ExpectedDate = ToDateTime(litter.ExpectedDate),
        BirthDate = ToDateTime(litter.BirthDate),
        Description = litter.Description,
        CreatedAt = litter.CreatedAt,
    };

    private static LitterEntity ToEntity(LitterTableEntity row) => new()
    {
        Id = Guid.Parse(row.RowKey),
        Name = row.Name,
        MotherId = row.MotherId,
        FatherId = row.FatherId,
        Status = Enum.Parse<LitterStatus>(row.Status),
        ExpectedDate = ToDateOnly(row.ExpectedDate),
        BirthDate = ToDateOnly(row.BirthDate),
        Description = row.Description,
        CreatedAt = row.CreatedAt,
    };

    private static DateTime? ToDateTime(DateOnly? date) =>
        date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static DateOnly? ToDateOnly(DateTime? date) =>
        date == null ? null : DateOnly.FromDateTime(date.Value.ToUniversalTime());
}
