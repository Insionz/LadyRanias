using Azure;
using Azure.Communication.Email;
// Azure-pakken har også en klasse som heter EmailMessage. Vi gir den et annet navn her, så den ikke forveksles med vår.
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;

namespace LadyRanias.App.Components.Contact.Impl;

/// <summary>Sender e-post via Azure Communication Services.</summary>
internal sealed class AcsEmailSender(EmailClient client, IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var sender = configuration["Email:SenderAddress"]
            ?? throw new InvalidOperationException("Email:SenderAddress mangler i konfigurasjonen");

        // Ren tekst, ikke HTML: det den besøkende har skrevet, skal aldri tolkes som HTML.
        var content = new EmailContent(email.Subject) { PlainText = email.Body };
        var message = new AcsEmailMessage(sender, email.To, content);

        // «Svar» i e-postprogrammet går til den som skrev, ikke til avsenderadressen (som ingen leser).
        message.ReplyTo.Add(new EmailAddress(email.ReplyTo));

        // WaitUntil.Completed venter til Azure har tatt imot og behandlet sendingen (vanligvis noen sekunder).
        // Feiler den, kastes et unntak, og bakgrunnsjobben prøver igjen senere.
        await client.SendAsync(WaitUntil.Completed, message, cancellationToken);
    }
}
