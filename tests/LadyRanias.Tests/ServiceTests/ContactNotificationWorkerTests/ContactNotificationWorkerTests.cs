using LadyRanias.App.Components.Contact.Api;
using LadyRanias.App.Components.Contact.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LadyRanias.Tests.ServiceTests.ContactNotificationWorkerTests;

[TestClass]
public sealed class ContactNotificationWorkerTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryContactDatabase _database = new();
    private readonly InMemoryNotificationQueue _queue = new();
    private readonly RecordingEmailSender _email = new();
    private readonly ContactNotificationWorker _worker;

    public ContactNotificationWorkerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:NotifyAddress"] = "mamma@example.com" })
            .Build();

        _worker = new ContactNotificationWorker(
            _queue, _database, _email, configuration, _clock, NullLogger<ContactNotificationWorker>.Instance);
    }

    private async Task<ContactMessageEntity> SaveAndQueueAsync()
    {
        var message = new ContactMessageEntity
        {
            Id = Guid.NewGuid(),
            Name = "Kari",
            Email = "kari@example.com",
            Subject = "Kull våren 2027",
            Message = "Har dere kattunger til våren?",
            Status = ContactStatusEnum.New,
            CreatedAt = Start,
        };
        await _database.SaveAsync(message, CancellationToken.None);
        await _queue.EnqueueAsync(message.Id, CancellationToken.None);
        return message;
    }

    [TestMethod]
    public async Task Empty_queue_returns_false()
    {
        Assert.IsFalse(await _worker.ProcessNextAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task Sends_email_marks_message_as_sent_and_removes_it_from_queue()
    {
        var message = await SaveAndQueueAsync();

        Assert.IsTrue(await _worker.ProcessNextAsync(CancellationToken.None));

        Assert.HasCount(1, _email.Sent);
        var email = _email.Sent[0];
        Assert.AreEqual("mamma@example.com", email.To);
        Assert.AreEqual("kari@example.com", email.ReplyTo, "«Svar» skal gå til den som skrev");
        StringAssert.Contains(email.Subject, "Kari");
        StringAssert.Contains(email.Body, "Har dere kattunger til våren?");

        var saved = await _database.GetAsync(message.Id, CancellationToken.None);
        Assert.IsNotNull(saved);
        Assert.AreEqual(Start, saved.EmailSentAt);
        Assert.HasCount(0, _queue.Pending);
    }

    [TestMethod]
    public async Task Failed_email_stays_in_queue_and_succeeds_on_retry()
    {
        var message = await SaveAndQueueAsync();
        _email.FailNext = 1;

        await _worker.ProcessNextAsync(CancellationToken.None);

        Assert.HasCount(0, _email.Sent);
        Assert.HasCount(1, _queue.Pending, "Meldingen skal ligge igjen og prøves på nytt");
        Assert.IsNull((await _database.GetAsync(message.Id, CancellationToken.None))?.EmailSentAt);

        await _worker.ProcessNextAsync(CancellationToken.None);

        Assert.HasCount(1, _email.Sent);
        Assert.HasCount(0, _queue.Pending);
    }

    [TestMethod]
    public async Task Message_that_keeps_failing_ends_in_poison_queue()
    {
        var message = await SaveAndQueueAsync();
        _email.FailNext = int.MaxValue;

        // MaxAttempts forsøk som feiler, og så én henting til, som flytter den til poison.
        for (var i = 0; i < ContactNotificationWorker.MaxAttempts + 1; i++)
        {
            await _worker.ProcessNextAsync(CancellationToken.None);
        }

        Assert.HasCount(0, _queue.Pending);
        CollectionAssert.AreEqual(new[] { message.Id }, _queue.Poison.ToArray());
        Assert.AreEqual(ContactNotificationWorker.MaxAttempts, _email.Attempts);
    }

    [TestMethod]
    public async Task Deleted_message_is_removed_from_queue_without_email()
    {
        var message = await SaveAndQueueAsync();
        await _database.DeleteAsync(message.Id, CancellationToken.None);

        await _worker.ProcessNextAsync(CancellationToken.None);

        Assert.HasCount(0, _email.Sent);
        Assert.HasCount(0, _queue.Pending);
    }

    [TestMethod]
    public async Task Already_sent_message_is_not_sent_twice()
    {
        // Som om appen krasjet etter at e-posten ble sendt, men før meldingen ble fjernet fra køen.
        var message = await SaveAndQueueAsync();
        await _database.SaveAsync(message with { EmailSentAt = Start }, CancellationToken.None);

        await _worker.ProcessNextAsync(CancellationToken.None);

        Assert.HasCount(0, _email.Sent);
        Assert.HasCount(0, _queue.Pending);
    }

    [TestMethod]
    public async Task Messages_older_than_a_year_are_deleted_newer_are_kept()
    {
        var old = await SaveAndQueueAsync();
        _clock.Advance(TimeSpan.FromDays(200));
        var recent = await SaveAndQueueAsync();
        await _database.SaveAsync(recent with { CreatedAt = _clock.GetUtcNow() }, CancellationToken.None);
        _clock.Advance(TimeSpan.FromDays(200)); // nå er den første 400 dager gammel, den andre 200

        var deleted = await _worker.DeleteOldMessagesAsync(CancellationToken.None);

        Assert.AreEqual(1, deleted);
        Assert.IsNull(await _database.GetAsync(old.Id, CancellationToken.None));
        Assert.IsNotNull(await _database.GetAsync(recent.Id, CancellationToken.None));
    }

    /// <summary>Husker e-postene i stedet for å sende dem, og kan feile et bestemt antall ganger.</summary>
    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];
        public int FailNext { get; set; }
        public int Attempts { get; private set; }

        public Task SendAsync(EmailMessage email, CancellationToken cancellationToken)
        {
            Attempts++;
            if (FailNext > 0)
            {
                FailNext--;
                throw new InvalidOperationException("E-posttjenesten er nede (test)");
            }

            Sent.Add(email);
            return Task.CompletedTask;
        }
    }
}
