using LadyRanias.App.Components.Contact.Impl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace LadyRanias.Tests.ComponentTests;

/// <summary>
/// Egen klasse med egen app og lav grense. Hadde testen delt app med ContactApiTests,
/// ville den brukt opp kvoten for de andre testene, som kjører samtidig.
/// </summary>
[TestClass]
public sealed class ContactRateLimitTests
{
    [TestMethod]
    public async Task Too_many_messages_from_same_ip_returns_too_many_requests()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Contact:RateLimit:PermitLimit", "2");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IContactDatabase, InMemoryContactDatabase>();
                    services.AddTestAuth();
                });
            });
        var client = factory.CreateClient();
        var message = new { name = "Kari", email = "kari@example.com", message = "Har dere kattunger til våren?" };

        var first = await client.PostAsJsonAsync("api/public/contact", message);
        var second = await client.PostAsJsonAsync("api/public/contact", message);
        var third = await client.PostAsJsonAsync("api/public/contact", message);

        Assert.AreEqual(HttpStatusCode.Accepted, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, second.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
