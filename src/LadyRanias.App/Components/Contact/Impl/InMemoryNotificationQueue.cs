namespace LadyRanias.App.Components.Contact.Impl;

/// <summary>
/// Kø i minnet, for tester. Enklere enn den ekte: en hentet melding blir ikke usynlig, den blir bare liggende
/// først i køen til den slettes. Det gjør testene forutsigbare: neste henting gir samme melding,
/// med DequeueCount én høyere, akkurat som når den ekte køen prøver på nytt.
/// </summary>
internal sealed class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Lock _lock = new();
    private readonly List<QueuedNotification> _pending = [];
    private readonly List<Guid> _poison = [];

    public IReadOnlyList<Guid> Pending
    {
        get { lock (_lock) return [.. _pending.Select(n => n.MessageId)]; }
    }

    public IReadOnlyList<Guid> Poison
    {
        get { lock (_lock) return [.. _poison]; }
    }

    public Task EnqueueAsync(Guid messageId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var queueId = Guid.NewGuid().ToString();
            _pending.Add(new QueuedNotification(messageId, queueId, PopReceipt: queueId, DequeueCount: 0));
        }
        return Task.CompletedTask;
    }

    public Task<QueuedNotification?> ReceiveAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_pending.Count == 0) return Task.FromResult<QueuedNotification?>(null);

            var received = _pending[0] with { DequeueCount = _pending[0].DequeueCount + 1 };
            _pending[0] = received;
            return Task.FromResult<QueuedNotification?>(received);
        }
    }

    public Task DeleteAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        lock (_lock) _pending.RemoveAll(n => n.QueueMessageId == notification.QueueMessageId);
        return Task.CompletedTask;
    }

    public Task MoveToPoisonAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _poison.Add(notification.MessageId);
            _pending.RemoveAll(n => n.QueueMessageId == notification.QueueMessageId);
        }
        return Task.CompletedTask;
    }
}
