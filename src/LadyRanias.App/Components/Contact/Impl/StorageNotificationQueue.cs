using Azure.Storage.Queues;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed class StorageNotificationQueue : INotificationQueue
{
    private readonly QueueClient _queueNotificationClient;
    private readonly QueueClient _queueNotificationPoisonClient;

    public StorageNotificationQueue(QueueServiceClient queueServiceClient) : this(queueServiceClient, "contact-notifications", "contact-notifications-poison") { }

    internal StorageNotificationQueue(QueueServiceClient queueServiceClient, string queueName, string queueNamePoison)
    {
        _queueNotificationClient = queueServiceClient.GetQueueClient(queueName);
        _queueNotificationPoisonClient = queueServiceClient.GetQueueClient(queueNamePoison);
        _queueNotificationClient.CreateIfNotExists();
        _queueNotificationPoisonClient.CreateIfNotExists();
    }

    public async Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await _queueNotificationClient.SendMessageAsync(messageId.ToString(), cancellationToken);
    }

    public async Task<QueuedNotification?> ReceiveAsync(CancellationToken cancellationToken)
    {
        var received = await _queueNotificationClient.ReceiveMessageAsync(visibilityTimeout: TimeSpan.FromMinutes(1), cancellationToken: cancellationToken);

        var queueMessage = received.Value;

        if (queueMessage == null) return null;

        return new QueuedNotification(
            // Vår id ligger i innholdet (Body). queueMessage.MessageId er køens egen id.
            MessageId: Guid.Parse(queueMessage.Body.ToString()),
            PopReceipt: queueMessage.PopReceipt,
            DequeueCount: queueMessage.DequeueCount,
            QueueMessageId: queueMessage.MessageId);
    }

    public async Task DeleteAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        await _queueNotificationClient.DeleteMessageAsync(notification.QueueMessageId, notification.PopReceipt, cancellationToken);
    }

    public async Task MoveToPoisonAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        await _queueNotificationPoisonClient.SendMessageAsync(notification.MessageId.ToString(), cancellationToken: cancellationToken);
        await _queueNotificationClient.DeleteMessageAsync(notification.QueueMessageId, notification.PopReceipt, cancellationToken);
    }
}
