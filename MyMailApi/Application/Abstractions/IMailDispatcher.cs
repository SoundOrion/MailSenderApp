using MyMailApi.Domain;

namespace MyMailApi.Application.Abstractions;

public interface IMailDispatcher
{
    Task DispatchAsync(MailMessageData message, CancellationToken cancellationToken = default);
}