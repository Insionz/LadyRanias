namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "E-post (ikke sendt, bare logget) til {To}, svar til {ReplyTo}\nEmne: {Subject}\n{Body}",
            email.To, email.ReplyTo, email.Subject, email.Body);

        return Task.CompletedTask;
    }
}
