using MyMailApi.Application.Abstractions;
using MyMailApi.Domain;

namespace MyMailApi.Application.Services;

public sealed class QueuedMailDispatcher : IMailDispatcher
{
    private readonly IMailQueue _mailQueue;

    public QueuedMailDispatcher(IMailQueue mailQueue)
    {
        _mailQueue = mailQueue;
    }

    public async Task DispatchAsync(MailMessageData message, CancellationToken cancellationToken = default)
    {
        await _mailQueue.EnqueueAsync(message, cancellationToken);
    }
}