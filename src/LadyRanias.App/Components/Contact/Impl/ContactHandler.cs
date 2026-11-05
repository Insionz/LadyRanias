using LadyRanias.App.Components.Contact.Api;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class ContactHandler(
    IContactDatabase database,
    TimeProvider timeProvider,
    ILogger<ContactHandler> logger) : IContactHandler
{
    public async Task CreateAsync(CreateContactMessageDto message, CancellationToken cancellationToken)
    {
        // Honeypot: feltet «Website» er skjult i skjemaet. Mennesker ser det ikke og lar det stå tomt.
        // Er det fylt ut, er det nesten sikkert en robot. Vi lagrer ingenting, men controlleren svarer
        // 202 som vanlig, så roboten ikke lærer at den ble oppdaget.
        if (!string.IsNullOrWhiteSpace(message.Website))
        {
            logger.LogInformation("Kontaktmelding forkastet: honeypot-feltet var fylt ut");
            return;
        }

        var entity = new ContactMessageEntity
        {
            Id = Guid.NewGuid(),
            // Trim fjerner mellomrom før og etter. Tomme valgfrie felt lagres som null, ikke som "".
            Name = message.Name.Trim(),
            Email = message.Email.Trim(),
            Phone = EmptyToNull(message.Phone),
            Subject = EmptyToNull(message.Subject),
            Message = message.Message.Trim(),
            Status = ContactStatusEnum.New,
            EmailSentAt = null,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await database.SaveAsync(entity, cancellationToken);

        // Del 3: her legges meldingens id i køen, så bakgrunnsjobben kan sende e-postvarselet.
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
