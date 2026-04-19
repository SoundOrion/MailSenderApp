using MyMailApi.Application.Abstractions;
using MyMailApi.Domain;

namespace MyMailApi.Application.Services;

public sealed class DirectMailDispatcher : IMailDispatcher
{
    private readonly IMailSender _mailSender;

    public DirectMailDispatcher(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task DispatchAsync(MailMessageData message, CancellationToken cancellationToken = default)
    {
        return _mailSender.SendAsync(message, cancellationToken);
    }
}