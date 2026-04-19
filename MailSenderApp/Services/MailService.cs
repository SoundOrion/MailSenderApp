using MailKit.Net.Smtp;
using MailKit.Security;
using MailSenderApp.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MailSenderApp.Services;

public sealed class MailService : IMailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailService> _logger;

    public MailService(
        IOptions<MailSettings> options,
        ILogger<MailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var message = CreateMessage(request);

        using var client = new SmtpClient();

        try
        {
            _logger.LogInformation("SMTP接続開始: {Host}:{Port}", _settings.Host, _settings.Port);

            var socketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                socketOptions,
                cancellationToken);

            await client.AuthenticateAsync(
                _settings.UserName,
                _settings.Password,
                cancellationToken);

            await client.SendAsync(message, cancellationToken);

            _logger.LogInformation(
                "メール送信成功: Subject={Subject}, To={ToCount}, Cc={CcCount}, Bcc={BccCount}",
                request.Subject,
                request.To.Count,
                request.Cc.Count,
                request.Bcc.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "メール送信失敗: Subject={Subject}", request.Subject);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    private MimeMessage CreateMessage(MailRequest request)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.UserName));

        foreach (var to in request.To)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        foreach (var cc in request.Cc)
        {
            message.Cc.Add(MailboxAddress.Parse(cc));
        }

        foreach (var bcc in request.Bcc)
        {
            message.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        message.Subject = request.Subject;

        ApplyPriority(message, request.Priority);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody
        };

        foreach (var filePath in request.AttachmentPaths)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"添付ファイルが見つかりません: {filePath}", filePath);
            }

            bodyBuilder.Attachments.Add(filePath);
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static void ValidateRequest(MailRequest request)
    {
        if (request.To.Count == 0 && request.Cc.Count == 0 && request.Bcc.Count == 0)
        {
            throw new InvalidOperationException("宛先が指定されていません。");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException("件名が指定されていません。");
        }

        var hasText = !string.IsNullOrWhiteSpace(request.TextBody);
        var hasHtml = !string.IsNullOrWhiteSpace(request.HtmlBody);

        if (!hasText && !hasHtml)
        {
            throw new InvalidOperationException("本文は TextBody または HtmlBody のどちらか一方以上が必要です。");
        }
    }

    private static void ApplyPriority(MimeMessage message, MailPriorityLevel priority)
    {
        switch (priority)
        {
            case MailPriorityLevel.High:
                message.Priority = MessagePriority.Urgent;
                message.Importance = MessageImportance.High;
                message.XPriority = XMessagePriority.High;
                break;

            case MailPriorityLevel.Low:
                message.Priority = MessagePriority.NonUrgent;
                message.Importance = MessageImportance.Low;
                message.XPriority = XMessagePriority.Low;
                break;

            default:
                message.Priority = MessagePriority.Normal;
                message.Importance = MessageImportance.Normal;
                message.XPriority = XMessagePriority.Normal;
                break;
        }
    }
}