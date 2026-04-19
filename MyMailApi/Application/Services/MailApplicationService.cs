using MyMailApi.Application.Abstractions;
using MyMailApi.Contracts;
using MyMailApi.Domain;

namespace MyMailApi.Application.Services;

public sealed class MailApplicationService : IMailApplicationService
{
    private readonly IMailSender _mailSender;
    private readonly IMailQueue _mailQueue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailApplicationService> _logger;

    public MailApplicationService(
        IMailSender mailSender,
        IMailQueue mailQueue,
        IConfiguration configuration,
        ILogger<MailApplicationService> logger)
    {
        _mailSender = mailSender;
        _mailQueue = mailQueue;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SendMailResponse> SendAsync(
        SendMailRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var message = MapToDomain(request);

        var mode = ResolveDispatchMode(request);

        switch (mode)
        {
            case MailDispatchMode.Queued:
                EnsureQueueSafe(message);
                await _mailQueue.EnqueueAsync(message, cancellationToken);

                _logger.LogInformation(
                    "メールをキュー投入: Subject={Subject}, To={ToCount}",
                    message.Subject,
                    message.To.Count);

                return new SendMailResponse
                {
                    Message = "Mail queued.",
                    Mode = mode.ToString()
                };

            default:
                await _mailSender.SendAsync(message, cancellationToken);

                _logger.LogInformation(
                    "メールを即送信: Subject={Subject}, To={ToCount}",
                    message.Subject,
                    message.To.Count);

                return new SendMailResponse
                {
                    Message = "Mail sent.",
                    Mode = mode.ToString()
                };
        }
    }

    private MailDispatchMode ResolveDispatchMode(SendMailRequest request)
    {
        if (request.Mode.HasValue)
        {
            return request.Mode.Value;
        }

        var defaultModeText = _configuration["MailDispatch:DefaultMode"] ?? "Direct";

        return Enum.TryParse<MailDispatchMode>(
            defaultModeText,
            ignoreCase: true,
            out var parsedMode)
            ? parsedMode
            : MailDispatchMode.Direct;
    }

    private static MailMessageData MapToDomain(SendMailRequest request)
    {
        var attachments = request.Attachments
            .Select(a => MailAttachment.FromBytes(
                a.FileName,
                Convert.FromBase64String(a.Base64Data),
                a.ContentType))
            .ToArray();

        return new MailMessageData
        {
            To = request.To,
            Cc = request.Cc,
            Bcc = request.Bcc,
            Subject = request.Subject,
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
            Priority = request.Priority,
            Attachments = attachments
        };
    }

    private static void ValidateRequest(SendMailRequest request)
    {
        if (request.To.Count == 0 && request.Cc.Count == 0 && request.Bcc.Count == 0)
        {
            throw new InvalidOperationException("宛先がありません。");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException("件名がありません。");
        }

        if (string.IsNullOrWhiteSpace(request.TextBody) &&
            string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            throw new InvalidOperationException("TextBody または HtmlBody のどちらかが必要です。");
        }

        foreach (var attachment in request.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.FileName))
            {
                throw new InvalidOperationException("添付ファイル名がありません。");
            }

            if (string.IsNullOrWhiteSpace(attachment.Base64Data))
            {
                throw new InvalidOperationException(
                    $"添付 '{attachment.FileName}' の Base64Data がありません。");
            }
        }
    }

    private static void EnsureQueueSafe(MailMessageData message)
    {
        foreach (var attachment in message.Attachments)
        {
            if (attachment.ContentStream is not null)
            {
                throw new InvalidOperationException(
                    "キュー送信では Stream 添付は使えません。byte[] を使ってください。");
            }
        }
    }
}