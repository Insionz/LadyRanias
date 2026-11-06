using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace LadyRanias.Tests.ComponentTests;

/// <summary>Helseadressene Azure bruker for å se om appen lever og er klar.</summary>
[TestClass]
public sealed class HealthTests
{
    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void StartApp(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();

        // Start appen nå, én gang (se CatApiTests).
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Live_returns_healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Ready_returns_healthy_when_storage_answers()
    {
        // Bruker ekte Table Storage, altså Azurite (som integrasjonstestene).
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Unknown_health_address_gives_404_not_the_react_page()
    {
        // Beviser at health/ er unntatt i fallback-ruten. Uten det hadde vi fått 200 og index.html.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/finnesikke");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
