using MailKit.Net.Smtp;
using MailKit.Security;
using MailSenderApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

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

        using var message = CreateMessage(request);

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
                "メール送信成功: Subject={Subject}, To={ToCount}, Attachments={AttachmentCount}",
                request.Subject,
                request.To.Count,
                request.Attachments.Count);
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
            message.To.Add(MailboxAddress.Parse(to));

        foreach (var cc in request.Cc)
            message.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in request.Bcc)
            message.Bcc.Add(MailboxAddress.Parse(bcc));

        message.Subject = request.Subject;

        ApplyPriority(message, request.Priority);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody
        };

        foreach (var attachment in request.Attachments)
        {
            AddAttachment(bodyBuilder, attachment);
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static void AddAttachment(BodyBuilder bodyBuilder, MailAttachment attachment)
    {
        attachment.Validate();

        MimeEntity mimeAttachment;

        if (attachment.Data is not null)
        {
            mimeAttachment = bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Data,
                GetContentType(attachment.ContentType));
        }
        else if (attachment.ContentStream is not null)
        {
            if (attachment.ContentStream.CanSeek)
            {
                attachment.ContentStream.Position = 0;
            }

            mimeAttachment = bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.ContentStream,
                GetContentType(attachment.ContentType));
        }
        else if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException(
                    $"添付ファイルが見つかりません: {attachment.FilePath}",
                    attachment.FilePath);
            }

            mimeAttachment = bodyBuilder.Attachments.Add(
                attachment.FileName,
                File.ReadAllBytes(attachment.FilePath),
                GetContentType(attachment.ContentType));
        }
        else
        {
            throw new InvalidOperationException("添付ファイルのソースが不正です。");
        }

        if (mimeAttachment is MimePart part)
        {
            part.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
            part.FileName = attachment.FileName;
        }
    }

    private static ContentType GetContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return new ContentType("application", "octet-stream");
        }

        return ContentType.Parse(contentType);
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

        foreach (var attachment in request.Attachments)
        {
            attachment.Validate();
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