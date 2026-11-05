using LadyRanias.App.Components.Contact.Api;
using LadyRanias.App.Components.Contact.Impl;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LadyRanias.Tests.ServiceTests.ContactHandlerTests;

[TestClass]
public sealed class ContactHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryContactDatabase _database = new();
    private readonly ContactHandler _handler;

    public ContactHandlerTests()
    {
        // NullLogger: en logger som ikke skriver noe. Testene bryr seg ikke om loggen.
        _handler = new ContactHandler(_database, _clock, NullLogger<ContactHandler>.Instance);
    }

    private static CreateContactMessageDto ValidMessage() => new()
    {
        Name = "Kari",
        Email = "kari@example.com",
        Message = "Har dere kattunger til våren?",
    };

    [TestMethod]
    public async Task Create_saves_message_as_new_without_email_sent()
    {
        await _handler.CreateAsync(ValidMessage(), CancellationToken.None);

        var all = await _database.GetAllAsync(CancellationToken.None);
        Assert.HasCount(1, all);
        Assert.AreEqual(ContactStatusEnum.New, all[0].Status);
        Assert.IsNull(all[0].EmailSentAt);
        Assert.AreEqual(Start, all[0].CreatedAt);
    }

    [TestMethod]
    public async Task Create_trims_text_and_stores_empty_optional_fields_as_null()
    {
        await _handler.CreateAsync(new CreateContactMessageDto
        {
            Name = "  Kari  ",
            Email = " kari@example.com ",
            Phone = "   ",
            Subject = "",
            Message = "  Har dere kattunger?  ",
        }, CancellationToken.None);

        var saved = (await _database.GetAllAsync(CancellationToken.None))[0];
        Assert.AreEqual("Kari", saved.Name);
        Assert.AreEqual("kari@example.com", saved.Email);
        Assert.AreEqual("Har dere kattunger?", saved.Message);
        Assert.IsNull(saved.Phone);
        Assert.IsNull(saved.Subject);
    }

    [TestMethod]
    public async Task Create_with_honeypot_filled_saves_nothing()
    {
        await _handler.CreateAsync(ValidMessage() with { Website = "http://spam.example" }, CancellationToken.None);

        Assert.HasCount(0, await _database.GetAllAsync(CancellationToken.None));
    }
}
