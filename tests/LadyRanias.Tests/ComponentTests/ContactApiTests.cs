using LadyRanias.App.Components.Contact.Api;
using LadyRanias.App.Components.Contact.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LadyRanias.Tests.ComponentTests;

[TestClass]
public sealed class ContactApiTests
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
            {
                // Alle testene her deler én IP-adresse. Høy grense, så de ikke stopper hverandre.
                // Selve grensen testes i ContactRateLimitTests.
                builder.UseSetting("Contact:RateLimit:PermitLimit", "1000");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IContactDatabase, InMemoryContactDatabase>();
                    services.AddSingleton<INotificationQueue, InMemoryNotificationQueue>();
                    services.AddTestAuth();
                });
            });
    }

    [ClassCleanup]
    public static void StopApp()
    {
        _factory.Dispose();
    }

    [TestInitialize]
    public void CreateClient()
    {
        // Ingen innlogging: kontaktskjemaet er åpent for alle.
        _httpClient = _factory.CreateClient();
    }

    [TestMethod]
    public async Task Valid_message_returns_accepted()
    {
        var response = await _httpClient.PostAsJsonAsync("api/public/contact", new
        {
            name = "Kari",
            email = "kari@example.com",
            message = "Har dere kattunger til våren?",
        });

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    }

    [TestMethod]
    public async Task Honeypot_filled_still_returns_accepted()
    {
        // Roboten skal ikke kunne se forskjell. At ingenting lagres, testes i ContactHandlerTests.
        var response = await _httpClient.PostAsJsonAsync("api/public/contact", new
        {
            name = "Robot",
            email = "robot@example.com",
            message = "Kjøp billige klokker her!",
            website = "http://spam.example",
        });

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    }

    [TestMethod]
    [DataRow("", "kari@example.com", "Har dere kattunger til våren?", "Name")]
    [DataRow("Kari", "ikke-en-epost", "Har dere kattunger til våren?", "Email")]
    [DataRow("Kari", "kari@example.com", "Hei", "Message")]
    public async Task Invalid_message_returns_bad_request_naming_the_field(string name, string email, string message, string field)
    {
        var response = await _httpClient.PostAsJsonAsync("api/public/contact", new { name, email, message });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), field);
    }

    [TestMethod]
    public async Task Too_long_message_returns_bad_request()
    {
        var response = await _httpClient.PostAsJsonAsync("api/public/contact", new
        {
            name = "Kari",
            email = "kari@example.com",
            message = new string('a', 4001),
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Sent_message_is_listed_for_admin_and_can_be_marked_answered_and_deleted()
    {
        var name = $"Kari {Guid.NewGuid():N}"; // unikt, siden testene deler database
        await _httpClient.PostAsJsonAsync("api/public/contact", new
        {
            name,
            email = "kari@example.com",
            message = "Har dere kattunger til våren?",
        });

        var admin = _factory.CreateClient().AsAdmin();
        var messages = await admin.GetFromJsonAsync<List<ContactMessageDto>>("api/admin/contact", Json);
        var mine = messages?.SingleOrDefault(m => m.Name == name);
        Assert.IsNotNull(mine);
        Assert.AreEqual(ContactStatusEnum.New, mine.Status);

        var statusResponse = await admin.PutAsJsonAsync($"api/admin/contact/{mine.Id}/status",
            new ChangeContactStatusDto { Status = ContactStatusEnum.Answered }, Json);
        Assert.AreEqual(HttpStatusCode.OK, statusResponse.StatusCode);

        var deleteResponse = await admin.DeleteAsync($"api/admin/contact/{mine.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
