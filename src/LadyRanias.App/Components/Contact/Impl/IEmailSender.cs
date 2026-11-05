namespace LadyRanias.App.Components.Contact.Impl;

internal interface IEmailSender
{
    Task SendAsync(EmailMessage email, CancellationToken cancellationToken);
}

internal sealed record EmailMessage(string To, string ReplyTo, string Subject, string Body);
