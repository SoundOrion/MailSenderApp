using MyMailApi.Contracts;

namespace MyMailApi.Application.Abstractions;

public interface IMailApplicationService
{
    Task<SendMailResponse> SendAsync(
        SendMailRequest request,
        CancellationToken cancellationToken = default);
}