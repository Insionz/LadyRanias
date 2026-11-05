using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using LadyRanias.App.Components.Litters.Api;
using LadyRanias.App.Components.Litters.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LadyRanias.Tests.ComponentTests;

[TestClass]
public sealed class LitterApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly DateOnly LastMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

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
                    services.AddSingleton<ILitterDatabase, InMemoryLitterDatabase>();
                    services.AddSingleton<IKittenDatabase, InMemoryKittenDatabase>();
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
    public async Task Full_flow_from_planned_litter_to_sold_kitten()
    {
        // Mor og far
        var mother = await CreateCatAsync("Kiara", SexEnum.Female);
        var father = await CreateCatAsync("Børre", SexEnum.Male);

        // Planlagt kull
        var createResponse = await _httpClient.PostAsJsonAsync("api/admin/litters", new CreateLitterDto
        {
            Name = "A-kullet",
            MotherId = mother.Id,
            FatherId = father.Id,
        }, Json);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var litter = await createResponse.Content.ReadFromJsonAsync<LitterDto>(Json);
        Assert.IsNotNull(litter);
        Assert.AreEqual(LitterStatus.Planned, litter.Status);

        // Kattunge før fødsel: 409
        var tooEarly = await _httpClient.PostAsJsonAsync($"api/admin/litters/{litter.Id}/kittens", NewKitten(), Json);
        Assert.AreEqual(HttpStatusCode.Conflict, tooEarly.StatusCode);

        // Født
        var bornResponse = await _httpClient.PostAsJsonAsync($"api/admin/litters/{litter.Id}/born", new MarkBornDto { BirthDate = LastMonth }, Json);
        Assert.AreEqual(HttpStatusCode.OK, bornResponse.StatusCode);

        // Født igjen: 409
        var bornAgain = await _httpClient.PostAsJsonAsync($"api/admin/litters/{litter.Id}/born", new MarkBornDto { BirthDate = LastMonth }, Json);
        Assert.AreEqual(HttpStatusCode.Conflict, bornAgain.StatusCode);

        // Kattunge
        var kittenResponse = await _httpClient.PostAsJsonAsync($"api/admin/litters/{litter.Id}/kittens", NewKitten(), Json);
        Assert.AreEqual(HttpStatusCode.Created, kittenResponse.StatusCode);
        var kitten = await kittenResponse.Content.ReadFromJsonAsync<KittenDto>(Json);
        Assert.IsNotNull(kitten);
        Assert.AreEqual(KittenStatus.Available, kitten.Status);

        // Reserver, selg, og prøv å reservere igjen
        Assert.AreEqual(HttpStatusCode.OK, (await ChangeStatusAsync(litter.Id, kitten.Id, KittenStatus.Reserved)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await ChangeStatusAsync(litter.Id, kitten.Id, KittenStatus.Sold)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await ChangeStatusAsync(litter.Id, kitten.Id, KittenStatus.Reserved)).StatusCode);

        // Den offentlige siden ser kullet med den solgte kattungen
        var publicLitter = await _httpClient.GetFromJsonAsync<LitterDto>($"api/public/litters/{litter.Id}", Json);
        Assert.IsNotNull(publicLitter);
        Assert.AreEqual(LitterStatus.Born, publicLitter.Status);
        Assert.AreEqual(LastMonth, publicLitter.BirthDate);
        Assert.HasCount(1, publicLitter.Kittens);
        Assert.AreEqual(KittenStatus.Sold, publicLitter.Kittens[0].Status);
    }

    [TestMethod]
    public async Task Create_with_unknown_mother_returns_bad_request()
    {
        var father = await CreateCatAsync("Børre", SexEnum.Male);

        var response = await _httpClient.PostAsJsonAsync("api/admin/litters", new CreateLitterDto
        {
            Name = "A-kullet",
            MotherId = Guid.NewGuid(),
            FatherId = father.Id,
        }, Json);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "MotherId");
    }

    [TestMethod]
    public async Task Change_status_with_unknown_value_returns_bad_request()
    {
        // JSON-konverteren avviser verdier som ikke finnes i enumen, før controlleren i det hele tatt kjører.
        var response = await _httpClient.PutAsync(
            $"api/admin/litters/{Guid.NewGuid()}/kittens/{Guid.NewGuid()}/status",
            JsonContent.Create(new { status = "Stolen" }));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Get_unknown_litter_returns_not_found()
    {
        var response = await _httpClient.GetAsync($"api/public/litters/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Mark_unknown_litter_born_returns_not_found()
    {
        var response = await _httpClient.PostAsJsonAsync($"api/admin/litters/{Guid.NewGuid()}/born", new MarkBornDto { BirthDate = LastMonth }, Json);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CatDto> CreateCatAsync(string name, SexEnum sex)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/cats", new CreateCatDto
        {
            Name = name,
            Sex = sex,
            BirthDate = new DateOnly(2022, 1, 1),
            Description = "Forelder",
        }, Json);
        response.EnsureSuccessStatusCode();

        var cat = await response.Content.ReadFromJsonAsync<CatDto>(Json);
        Assert.IsNotNull(cat);
        return cat;
    }

    private static CreateKittenDto NewKitten() => new() { Name = "Ask", Sex = SexEnum.Male };

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid litterId, Guid kittenId, KittenStatus status)
    {
        return _httpClient.PutAsJsonAsync(
            $"api/admin/litters/{litterId}/kittens/{kittenId}/status",
            new ChangeKittenStatusDto { Status = status },
            Json);
    }
}
