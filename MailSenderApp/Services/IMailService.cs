using MailSenderApp.Models;

namespace MailSenderApp.Services;

public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken cancellationToken = default);
}