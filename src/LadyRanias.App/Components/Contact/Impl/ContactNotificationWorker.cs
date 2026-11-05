using System.Text;

namespace LadyRanias.App.Components.Contact.Impl;

/// <summary>
/// Henter meldinger fra køen og sender e-postvarsel til oppdretter.
///
/// Ingenting går tapt: en hentet melding er bare usynlig i ett minutt. Sletter vi den ikke innen da
/// (fordi e-posten feilet eller appen krasjet), blir den synlig igjen og prøves på nytt.
/// Etter MaxAttempts forsøk flyttes den til poison-køen, så en ødelagt melding ikke prøves i det uendelige.
/// </summary>
internal sealed class ContactNotificationWorker(
    INotificationQueue queue,
    IContactDatabase database,
    IEmailSender emailSender,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ContactNotificationWorker> logger) : BackgroundService
{
    public const int MaxAttempts = 5;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);

    /// <summary>Hvor lenge meldinger tas vare på (personvern). Eldre meldinger slettes automatisk.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(365);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
    private DateTimeOffset _lastCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Én gang i døgnet: slett meldinger som er eldre enn et år.
                if (timeProvider.GetUtcNow() - _lastCleanup > CleanupInterval)
                {
                    await DeleteOldMessagesAsync(stoppingToken);
                    _lastCleanup = timeProvider.GetUtcNow();
                }

                var processed = await ProcessNextAsync(stoppingToken);
                if (processed) continue; // det kan ligge flere, hent neste med en gang
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // appen stopper
            }
            catch (Exception ex)
            {
                // For eksempel at Storage ikke svarer. Et ufanget unntak her ville stoppet hele appen,
                // så vi logger og prøver igjen etter pausen.
                logger.LogError(ex, "Feil i bakgrunnsjobben for kontaktvarsler");
            }

            // Køen er tom (eller noe feilet): vent litt, så vi ikke spør Storage hundre ganger i sekundet.
            try
            {
                await Task.Delay(IdleDelay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Behandler én melding fra køen. false = køen var tom. internal, så testene kan kjøre én runde om gangen.</summary>
    internal async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var notification = await queue.ReceiveAsync(ct);
        if (notification == null)
        {
            return false;
        }

        if (notification.DequeueCount > MaxAttempts)
        {
            await queue.MoveToPoisonAsync(notification, ct);
            logger.LogError("Kontaktvarsel for melding {MessageId} feilet {Attempts} ganger og er flyttet til poison-køen",
                notification.MessageId, notification.DequeueCount - 1);
            return true;
        }

        var message = await database.GetAsync(notification.MessageId, ct);

        // Slettet i mellomtiden, eller allerede sendt (appen krasjet etter sending, men før køen ble ryddet).
        // I begge tilfeller er det ingenting å gjøre, bortsett fra å fjerne den fra køen.
        if (message == null || message.EmailSentAt != null)
        {
            await queue.DeleteAsync(notification, ct);
            return true;
        }

        try
        {
            await emailSender.SendAsync(BuildEmail(message), ct);

            await database.SaveAsync(message with { EmailSentAt = timeProvider.GetUtcNow() }, ct);
            await queue.DeleteAsync(notification, ct);

            logger.LogInformation("Kontaktvarsel sendt for melding {MessageId}", message.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ikke slett: meldingen blir synlig igjen etter timeouten og prøves på nytt.
            logger.LogWarning(ex, "Kunne ikke sende kontaktvarsel for melding {MessageId} (forsøk {Attempt} av {Max})",
                message.Id, notification.DequeueCount, MaxAttempts);
        }

        return true;
    }

    /// <summary>Sletter meldinger eldre enn Retention. Gir tilbake hvor mange som ble slettet.</summary>
    internal async Task<int> DeleteOldMessagesAsync(CancellationToken ct)
    {
        var cutoff = timeProvider.GetUtcNow() - Retention;
        var old = (await database.GetAllAsync(ct)).Where(m => m.CreatedAt < cutoff).ToList();

        foreach (var message in old)
        {
            await database.DeleteAsync(message.Id, ct);
        }

        if (old.Count > 0)
        {
            logger.LogInformation("Slettet {Count} kontaktmeldinger eldre enn {Days} dager", old.Count, Retention.Days);
        }

        return old.Count;
    }

    private EmailMessage BuildEmail(ContactMessageEntity message)
    {
        // Mangler adressen lokalt, går det greit: LogEmailSender skriver bare til loggen.
        // I produksjon sjekkes den ved oppstart (del 4).
        var notifyAddress = configuration["Email:NotifyAddress"] ?? "oppdretter@example.invalid";

        var subject = string.IsNullOrWhiteSpace(message.Subject)
            ? $"Ny melding fra {message.Name}"
            : $"Ny melding fra {message.Name}: {message.Subject}";

        // Ren tekst, ikke HTML: det den besøkende har skrevet, skal aldri tolkes som HTML.
        var body = new StringBuilder()
            .AppendLine("Ny melding fra kontaktskjemaet på LadyRanias.")
            .AppendLine()
            .AppendLine($"Navn:    {message.Name}")
            .AppendLine($"E-post:  {message.Email}")
            .AppendLine($"Telefon: {message.Phone ?? "ikke oppgitt"}")
            .AppendLine($"Emne:    {message.Subject ?? "ikke oppgitt"}")
            .AppendLine()
            .AppendLine(message.Message)
            .AppendLine()
            .AppendLine($"Trykk «Svar» for å svare. Svaret går rett til {message.Email}.")
            .ToString();

        return new EmailMessage(To: notifyAddress, ReplyTo: message.Email, Subject: subject, Body: body);
    }
}
