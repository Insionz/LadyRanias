namespace LadyRanias.App.Components.Contact.Impl;

internal interface INotificationQueue
{
    Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken);
    Task<QueuedNotification?> ReceiveAsync(CancellationToken cancellationToken);
    Task DeleteAsync(QueuedNotification notification, CancellationToken cancellationToken);
    Task MoveToPoisonAsync(QueuedNotification notification, CancellationToken cancellationToken);
}

internal sealed record QueuedNotification(Guid MessageId, string QueueMessageId, string PopReceipt, long DequeueCount);
