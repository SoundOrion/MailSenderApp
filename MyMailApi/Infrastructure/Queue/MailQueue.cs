using System.Threading.Channels;
using MyMailApi.Application.Abstractions;
using MyMailApi.Domain;

namespace MyMailApi.Infrastructure.Queue;

public sealed class MailQueue : IMailQueue
{
    private readonly ChannelWriter<MailMessageData> _writer;

    public MailQueue(Channel<MailMessageData> channel)
    {
        _writer = channel.Writer;
    }

    public async ValueTask EnqueueAsync(MailMessageData message, CancellationToken cancellationToken = default)
    {
        await _writer.WriteAsync(message, cancellationToken);
    }
}