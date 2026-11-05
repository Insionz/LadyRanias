using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace LadyRanias.App.Components.Storage;

public static class StorageComponent
{
    public static WebApplicationBuilder UseStorage(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Storage");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Lokalt (Azurite), i tester og i CI: tilkoblingsstreng.

            builder.Services.AddSingleton(new TableServiceClient(connectionString));
            builder.Services.AddSingleton(new BlobServiceClient(connectionString));
            builder.Services.AddSingleton(new QueueServiceClient(connectionString));
        }
        else
        {
            // I Azure: ingen nøkler. Appen logger inn med sin egen identitet.
            var account = builder.Configuration["Storage:AccountName"]
                ?? throw new InvalidOperationException("Verken ConnectionStrings:Storage eller Storage:AccountName er satt");
            var credential = new DefaultAzureCredential();

            builder.Services.AddSingleton(new TableServiceClient(new Uri($"https://{account}.table.core.windows.net"), credential));
            builder.Services.AddSingleton(new BlobServiceClient(new Uri($"https://{account}.blob.core.windows.net"), credential));
            builder.Services.AddSingleton(new QueueServiceClient(new Uri($"https://{account}.queue.core.windows.net"), credential));
        }

        return builder;
    }
}