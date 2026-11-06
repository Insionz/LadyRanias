using Azure.Data.Tables;
using LadyRanias.App.Components.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LadyRanias.Tests.ServiceTests.StorageHealthCheckTests;

[TestClass]
public sealed class StorageHealthCheckTests
{
    // Azurites offentlige standardkonto (står i Microsoft-dokumentasjonen, ikke en hemmelighet),
    // men med port 1, der ingenting svarer. Slik later vi som lagringen er nede.
    private const string UnreachableStorage =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
        "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
        "TableEndpoint=http://127.0.0.1:1/devstoreaccount1";

    [TestMethod]
    public async Task Is_unhealthy_when_table_storage_does_not_answer()
    {
        // MaxRetries = 0: ellers prøver SDK-et på nytt flere ganger, og testen tar mange sekunder.
        var client = new TableServiceClient(UnreachableStorage, new TableClientOptions { Retry = { MaxRetries = 0 } });
        var check = new StorageHealthCheck(client);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task Is_healthy_when_table_storage_answers()
    {
        // Ekte Azurite.
        var client = new TableServiceClient("UseDevelopmentStorage=true");
        var check = new StorageHealthCheck(client);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
    }
}
