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

[TestClass]
public sealed class CatApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;


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

        // Start appen nå, én gang. Ellers kan to tester som kjører samtidig starte hver sin app
        // (med hver sin database i minnet) første gang de ber om en klient.
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    [TestInitialize]
    public void CreateClient()
    {
        _httpClient = _factory.CreateClient().AsAdmin();
    }

    [TestMethod]
    public async Task Create_then_get_returns_the_cat()
    {
        var newCat = new CreateCatDto
        {
            Name = "Mons",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 6, 1),
            Description = "Testkatt",
        };

        var createResponse = await _httpClient.PostAsJsonAsync("api/admin/cats", newCat, Json);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatDto>(Json);
        Assert.IsNotNull(created);

        var fetched = await _httpClient.GetFromJsonAsync<CatDto>($"api/public/cats/{created.Id}", Json);
        Assert.IsNotNull(fetched);
        Assert.AreEqual("Mons", fetched.Name);
        Assert.AreEqual(SexEnum.Male, fetched.Sex);
    }

    [TestMethod]
    public async Task Get_with_unknown_id_returns_not_found()
    {
        var response = await _httpClient.GetAsync($"api/public/cats/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Post_with_empty_name_returns_bad_request()
    {
        var newCat = new CreateCatDto
        {
            Name = "",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 6, 1),
            Description = "Testkatt",
        };

        var response = await _httpClient.PostAsJsonAsync("api/admin/cats", newCat, Json);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Create_then_delete_cat_returns_no_content_then_get_returns_not_found()
    {
        var newCat = new CreateCatDto
        {
            Name = "Mons",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 6, 1),
            Description = "Testkatt",
        };

        var createResponse = await _httpClient.PostAsJsonAsync($"api/admin/cats", newCat, Json);

        var created = await createResponse.Content.ReadFromJsonAsync<CatDto>(Json);
        Assert.IsNotNull(created);

        var deleteResponse = await _httpClient.DeleteAsync($"api/admin/cats/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var response = await _httpClient.GetAsync($"api/public/cats/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Get_all_using_post_returns_method_not_allowed()
    {
        var response = await _httpClient.PostAsync($"api/public/cats", null);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Post_with_future_birthdate_returns_bad_request()
    {
        var birthDateTomorrow = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime).AddDays(1);

        var catBornTomorrow = new CreateCatDto
        {
            Name = "Test",
            Sex = SexEnum.Male,
            BirthDate = birthDateTomorrow,
            Description = "Test"
        };

        var response = await _httpClient.PostAsJsonAsync("api/admin/cats", catBornTomorrow, Json);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "BirthDate");
    }

    [TestMethod]
    public async Task Update_then_get_returns_the_updated_cat()
    {
        var createResponse = await _httpClient.PostAsJsonAsync("api/admin/cats", new CreateCatDto
        {
            Name = "Mons",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 6, 1),
            Description = "Testkatt",
        }, Json);
        var created = await createResponse.Content.ReadFromJsonAsync<CatDto>(Json);
        Assert.IsNotNull(created);

        var updateResponse = await _httpClient.PutAsJsonAsync($"api/admin/cats/{created.Id}", new UpdateCatDto
        {
            Name = "Mons Store",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 5, 20),
            Description = "Har vokst",
        }, Json);
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);

        var fetched = await _httpClient.GetFromJsonAsync<CatDto>($"api/public/cats/{created.Id}", Json);
        Assert.IsNotNull(fetched);
        Assert.AreEqual("Mons Store", fetched.Name);
        Assert.AreEqual(new DateOnly(2024, 5, 20), fetched.BirthDate);
        Assert.AreEqual("Har vokst", fetched.Description);
    }

    [TestMethod]
    public async Task Update_unknown_cat_returns_not_found()
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/cats/{Guid.NewGuid()}", new UpdateCatDto
        {
            Name = "Ingen",
            Sex = SexEnum.Male,
            BirthDate = new DateOnly(2024, 5, 20),
            Description = "Finnes ikke",
        }, Json);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Unknown_api_route_returns_not_found()
    {
        var response = await _httpClient.GetAsync("api/finnes-ikke");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
