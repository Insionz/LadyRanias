using LadyRanias.App.Components.Cats.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace LadyRanias.Tests.ComponentTests;

/// <summary>ladyranias.no (uten www) skal sendes videre til www.ladyranias.no.</summary>
[TestClass]
public sealed class DomainRedirectTests
{
    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void StartApp(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["CanonicalHost"] = "www.ladyranias.no" }));
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<ICatDatabase, InMemoryCatDatabase>();
                    services.AddTestAuth();
                });
            });

        // Start appen nå, én gang (se CatApiTests).
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    private static HttpClient ClientFor(string host)
    {
        // Ikke følg omdirigeringen: vi vil se selve svaret (301 og hvor det peker).
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Host = host;
        return client;
    }

    [TestMethod]
    public async Task Bare_domain_is_redirected_permanently_to_www_with_path_and_query()
    {
        var response = await ClientFor("ladyranias.no").GetAsync("katter?side=2");

        Assert.AreEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.AreEqual("https://www.ladyranias.no/katter?side=2", response.Headers.Location?.ToString());
    }

    [TestMethod]
    public async Task Www_is_not_redirected()
    {
        var response = await ClientFor("www.ladyranias.no").GetAsync("api/public/cats");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Azure_address_is_not_redirected()
    {
        var response = await ClientFor("ca-ladyranias-prod.example.azurecontainerapps.io").GetAsync("api/public/cats");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
