using MyMailApi.Domain;

namespace MyMailApi.Application.Abstractions;

public interface IMailSender
{
    Task SendAsync(MailMessageData message, CancellationToken cancellationToken = default);
}