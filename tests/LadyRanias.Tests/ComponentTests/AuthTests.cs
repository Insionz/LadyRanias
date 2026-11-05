using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LadyRanias.Tests.ComponentTests;

/// <summary>Hvem slipper inn hvor. Selve funksjonaliteten testes i de andre komponenttestene.</summary>
[TestClass]
public sealed class AuthTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void StartApp(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<ICatDatabase, InMemoryCatDatabase>();
                    services.AddTestAuth();
                }));
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    private static CreateCatDto NewCat() => new()
    {
        Name = "Mons",
        Sex = SexEnum.Male,
        BirthDate = new DateOnly(2024, 6, 1),
        Description = "Testkatt",
    };

    [TestMethod]
    public async Task Admin_without_login_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/admin/cats", NewCat(), Json);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Admin_logged_in_without_role_returns_forbidden()
    {
        var client = _factory.CreateClient().As("nabo@example.com");

        var response = await client.PostAsJsonAsync("api/admin/cats", NewCat(), Json);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Admin_with_admin_role_is_allowed()
    {
        var client = _factory.CreateClient().AsAdmin();

        var response = await client.PostAsJsonAsync("api/admin/cats", NewCat(), Json);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    // Alle tre admin-controllerne skal være beskyttet. Glemmer noen [Authorize] på en ny controller, blir denne rød.
    [TestMethod]
    [DataRow("GET", "api/admin/cats")]
    [DataRow("DELETE", "api/admin/cats/00000000-0000-0000-0000-000000000001")]
    [DataRow("DELETE", "api/admin/cats/00000000-0000-0000-0000-000000000001/images/00000000-0000-0000-0000-000000000002")]
    [DataRow("GET", "api/admin/litters/00000000-0000-0000-0000-000000000001")]
    [DataRow("DELETE", "api/admin/litters/00000000-0000-0000-0000-000000000001")]
    public async Task Every_admin_controller_requires_login(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Public_api_is_open_without_login()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/public/cats");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Me_without_login_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/auth/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Me_shows_name_and_admin_flag()
    {
        var client = _factory.CreateClient().AsAdmin();

        var response = await client.GetStringAsync("api/auth/me");

        StringAssert.Contains(response, "mamma@example.com");
        StringAssert.Contains(response, "\"isAdmin\":true");
    }
}
