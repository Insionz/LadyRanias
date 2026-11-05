using Azure.Data.Tables;
using LadyRanias.App.Components.Contact.Api;
using LadyRanias.App.Components.Contact.Impl;

namespace LadyRanias.Tests.IntegrationTests;

[TestClass]
public sealed class TableContactDatabaseTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private TableServiceClient _serviceClient = null!;
    private string _tableName = null!;
    private TableContactDatabase _database = null!;

    [TestInitialize]
    public void OnSetup()
    {
        _tableName = $"testcontact{Guid.NewGuid():N}";
        _serviceClient = new TableServiceClient("UseDevelopmentStorage=true");
        _database = new TableContactDatabase(_serviceClient, _tableName);
    }

    [TestCleanup]
    public async Task DeleteTableAsync()
    {
        await _serviceClient.DeleteTableAsync(_tableName);
    }

    private static ContactMessageEntity NewMessage(string name = "Kari") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = "kari@example.com",
        Phone = null,
        Subject = "Kull våren 2027",
        Message = "Har dere kattunger til våren?",
        Status = ContactStatusEnum.New,
        EmailSentAt = null,
        CreatedAt = Start,
    };

    [TestMethod]
    public async Task Save_then_get_returns_same_message()
    {
        var message = NewMessage();

        await _database.SaveAsync(message, CancellationToken.None);

        Assert.AreEqual(message, await _database.GetAsync(message.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Save_with_email_sent_and_new_status_overwrites_it()
    {
        // Slik bakgrunnsjobben (del 3) og adminsiden (del 5) kommer til å oppdatere meldingen.
        var message = NewMessage();
        await _database.SaveAsync(message, CancellationToken.None);

        var updated = message with { Status = ContactStatusEnum.Read, EmailSentAt = Start.AddMinutes(1) };
        await _database.SaveAsync(updated, CancellationToken.None);

        Assert.AreEqual(updated, await _database.GetAsync(message.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetAll_returns_newest_first()
    {
        var older = NewMessage("Eldre");
        var newer = NewMessage("Nyere") with { CreatedAt = Start.AddHours(1) };
        await _database.SaveAsync(older, CancellationToken.None);
        await _database.SaveAsync(newer, CancellationToken.None);

        var all = await _database.GetAllAsync(CancellationToken.None);

        Assert.HasCount(2, all);
        Assert.AreEqual("Nyere", all[0].Name);
        Assert.AreEqual("Eldre", all[1].Name);
    }

    [TestMethod]
    public async Task Delete_removes_message_and_returns_false_the_second_time()
    {
        var message = NewMessage();
        await _database.SaveAsync(message, CancellationToken.None);

        Assert.IsTrue(await _database.DeleteAsync(message.Id, CancellationToken.None));
        Assert.IsNull(await _database.GetAsync(message.Id, CancellationToken.None));
        Assert.IsFalse(await _database.DeleteAsync(message.Id, CancellationToken.None));
    }

    [TestMethod]
    public async Task Get_unknown_id_returns_null()
    {
        Assert.IsNull(await _database.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
