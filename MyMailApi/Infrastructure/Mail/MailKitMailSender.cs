using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MyMailApi.Application.Abstractions;
using MyMailApi.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyMailApi.Infrastructure.Mail;

public sealed class MailKitMailSender : IMailSender
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailKitMailSender> _logger;

    public MailKitMailSender(
        IOptions<MailSettings> options,
        ILogger<MailKitMailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailMessageData message, CancellationToken cancellationToken = default)
    {
        ValidateMessage(message);

        using var mimeMessage = BuildMimeMessage(message);
        using var smtp = new SmtpClient();

        smtp.Timeout = _settings.TimeoutMilliseconds;

        try
        {
            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            _logger.LogInformation(
                "SMTP接続開始: Host={Host}, Port={Port}, Security={Security}",
                _settings.Host,
                _settings.Port,
                secureSocketOptions);

            await smtp.ConnectAsync(
                _settings.Host,
                _settings.Port,
                secureSocketOptions,
                cancellationToken);

            await smtp.AuthenticateAsync(
                _settings.UserName,
                _settings.Password,
                cancellationToken);

            await smtp.SendAsync(mimeMessage, cancellationToken);

            _logger.LogInformation(
                "メール送信成功: Subject={Subject}, To={ToCount}, Cc={CcCount}, Bcc={BccCount}, Attachments={AttachmentCount}",
                message.Subject,
                message.To.Count,
                message.Cc.Count,
                message.Bcc.Count,
                message.Attachments.Count);
        }
        finally
        {
            if (smtp.IsConnected)
            {
                await smtp.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    private MimeMessage BuildMimeMessage(MailMessageData message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(_settings.DisplayName, _settings.UserName));

        foreach (var to in message.To)
            mimeMessage.To.Add(MailboxAddress.Parse(to));

        foreach (var cc in message.Cc)
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in message.Bcc)
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));

        mimeMessage.Subject = message.Subject;

        ApplyPriority(mimeMessage, message.Priority);

        var builder = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        };

        foreach (var attachment in message.Attachments)
        {
            AddAttachment(builder, attachment);
        }

        mimeMessage.Body = builder.ToMessageBody();

        return mimeMessage;
    }

    private static void AddAttachment(BodyBuilder builder, MailAttachment attachment)
    {
        attachment.Validate();

        var contentType = GetContentType(attachment.ContentType);

        if (attachment.Data is not null)
        {
            builder.Attachments.Add(
                attachment.FileName,
                attachment.Data,
                contentType);
            return;
        }

        if (attachment.ContentStream is not null)
        {
            if (attachment.ContentStream.CanSeek)
            {
                attachment.ContentStream.Position = 0;
            }

            builder.Attachments.Add(
                attachment.FileName,
                attachment.ContentStream,
                contentType);
            return;
        }

        if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException(
                    $"添付ファイルが見つかりません: {attachment.FilePath}",
                    attachment.FilePath);
            }

            builder.Attachments.Add(
                attachment.FileName,
                File.ReadAllBytes(attachment.FilePath),
                contentType);
            return;
        }

        throw new InvalidOperationException("添付ファイルのソースが不正です。");
    }

    private static ContentType GetContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? new ContentType("application", "octet-stream")
            : ContentType.Parse(contentType);
    }

    private static void ValidateMessage(MailMessageData message)
    {
        if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
        {
            throw new InvalidOperationException("宛先が指定されていません。");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            throw new InvalidOperationException("件名が指定されていません。");
        }

        if (string.IsNullOrWhiteSpace(message.TextBody) &&
            string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new InvalidOperationException("TextBody または HtmlBody のいずれかが必要です。");
        }

        foreach (var attachment in message.Attachments)
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