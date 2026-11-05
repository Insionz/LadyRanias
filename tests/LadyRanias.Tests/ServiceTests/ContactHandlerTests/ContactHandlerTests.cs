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
    private readonly InMemoryNotificationQueue _queue = new();
    private readonly ContactHandler _handler;

    public ContactHandlerTests()
    {
        // NullLogger: en logger som ikke skriver noe. Testene bryr seg ikke om loggen.
        _handler = new ContactHandler(_database, _clock, NullLogger<ContactHandler>.Instance, _queue);
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
    public async Task Create_puts_the_saved_message_id_in_the_queue()
    {
        await _handler.CreateAsync(ValidMessage(), CancellationToken.None);

        var saved = (await _database.GetAllAsync(CancellationToken.None))[0];
        CollectionAssert.AreEqual(new[] { saved.Id }, _queue.Pending.ToArray());
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
    public async Task Create_with_honeypot_filled_saves_nothing_and_queues_nothing()
    {
        await _handler.CreateAsync(ValidMessage() with { Website = "http://spam.example" }, CancellationToken.None);

        Assert.HasCount(0, await _database.GetAllAsync(CancellationToken.None));
        Assert.HasCount(0, _queue.Pending);
    }

    [TestMethod]
    public async Task GetAll_returns_newest_first()
    {
        await _handler.CreateAsync(ValidMessage() with { Name = "Eldre" }, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(5));
        await _handler.CreateAsync(ValidMessage() with { Name = "Nyere" }, CancellationToken.None);

        var all = await _handler.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, all);
        Assert.AreEqual("Nyere", all[0].Name);
        Assert.AreEqual("Eldre", all[1].Name);
    }

    [TestMethod]
    public async Task ChangeStatus_saves_new_status()
    {
        await _handler.CreateAsync(ValidMessage(), CancellationToken.None);
        var id = (await _handler.GetAllAsync(CancellationToken.None))[0].Id;

        var changed = await _handler.ChangeStatusAsync(id, ContactStatusEnum.Answered, CancellationToken.None);

        Assert.IsNotNull(changed);
        Assert.AreEqual(ContactStatusEnum.Answered, changed.Status);
        Assert.AreEqual(ContactStatusEnum.Answered, (await _handler.GetAllAsync(CancellationToken.None))[0].Status);
    }

    [TestMethod]
    public async Task ChangeStatus_of_unknown_message_returns_null()
    {
        Assert.IsNull(await _handler.ChangeStatusAsync(Guid.NewGuid(), ContactStatusEnum.Read, CancellationToken.None));
    }

    [TestMethod]
    public async Task Delete_removes_message()
    {
        await _handler.CreateAsync(ValidMessage(), CancellationToken.None);
        var id = (await _handler.GetAllAsync(CancellationToken.None))[0].Id;

        Assert.IsTrue(await _handler.DeleteAsync(id, CancellationToken.None));
        Assert.HasCount(0, await _handler.GetAllAsync(CancellationToken.None));
    }
}
