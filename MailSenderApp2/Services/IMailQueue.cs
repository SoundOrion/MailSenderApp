using MailSenderApp.Models;

namespace MailSenderApp.Services;

public interface IMailQueue
{
    ValueTask QueueAsync(MailRequest request, CancellationToken cancellationToken = default);
}