using LadyRanias.App.Components.Contact.Api;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class ContactHandler(
    IContactDatabase database,
    TimeProvider timeProvider,
    ILogger<ContactHandler> logger,
    INotificationQueue notificationQueue) : IContactHandler
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
            Name = message.Name.Trim(),
            Email = message.Email.Trim(),
            Phone = EmptyToNull(message.Phone),
            Subject = EmptyToNull(message.Subject),
            Message = message.Message.Trim(),
            Status = ContactStatusEnum.New,
            EmailSentAt = null,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        // Lagre først, så legg i køen. Feiler køen, ligger meldingen likevel trygt i tabellen
        // og vises på adminsiden. Bare e-postvarselet uteblir.
        await database.SaveAsync(entity, cancellationToken);
        await notificationQueue.EnqueueAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ContactMessageDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var all = await database.GetAllAsync(cancellationToken);

        return [.. all.OrderByDescending(m => m.CreatedAt).Select(ToDto)];
    }

    public async Task<ContactMessageDto?> ChangeStatusAsync(Guid id, ContactStatusEnum status, CancellationToken cancellationToken)
    {
        var message = await database.GetAsync(id, cancellationToken);
        if (message == null) return null;

        var changed = message with { Status = status };
        await database.SaveAsync(changed, cancellationToken);
        return ToDto(changed);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return database.DeleteAsync(id, cancellationToken);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContactMessageDto ToDto(ContactMessageEntity message) => new()
    {
        Id = message.Id,
        Name = message.Name,
        Email = message.Email,
        Phone = message.Phone,
        Subject = message.Subject,
        Message = message.Message,
        Status = message.Status,
        EmailSentAt = message.EmailSentAt,
        CreatedAt = message.CreatedAt,
    };
}
