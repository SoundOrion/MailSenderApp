using System.Threading.Channels;
using MailSenderApp.Models;

namespace MailSenderApp.Services;

public sealed class MailQueue : IMailQueue
{
    private readonly ChannelWriter<MailRequest> _writer;

    public MailQueue(Channel<MailRequest> channel)
    {
        _writer = channel.Writer;
    }

    public async ValueTask QueueAsync(MailRequest request, CancellationToken cancellationToken = default)
    {
        await _writer.WriteAsync(request, cancellationToken);
    }
}