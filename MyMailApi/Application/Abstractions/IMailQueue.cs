using MyMailApi.Domain;

namespace MyMailApi.Application.Abstractions;

public interface IMailQueue
{
    ValueTask EnqueueAsync(MailMessageData message, CancellationToken cancellationToken = default);
}