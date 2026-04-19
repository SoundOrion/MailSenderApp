using MailKit.Net.Smtp;
using MailKit.Security;
using MailSenderApp.Models;
using Microsoft.Extensions.Logging;
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
                "メール送信成功: Subject={Subject}, To={ToCount}",
                request.Subject,
                request.To.Count);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }

            message.Dispose();
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

        if (attachment.Data is not null)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Data,
                GetContentType(attachment.ContentType));
            return;
        }

        if (attachment.ContentStream is not null)
        {
            if (attachment.ContentStream.CanSeek)
                attachment.ContentStream.Position = 0;

            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.ContentStream,
                GetContentType(attachment.ContentType));
            return;
        }

        if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            if (!File.Exists(attachment.FilePath))
                throw new FileNotFoundException($"添付ファイルが見つかりません: {attachment.FilePath}");

            bodyBuilder.Attachments.Add(
                attachment.FileName,
                File.ReadAllBytes(attachment.FilePath),
                GetContentType(attachment.ContentType));
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

    private static void ValidateRequest(MailRequest request)
    {
        if (request.To.Count == 0 && request.Cc.Count == 0 && request.Bcc.Count == 0)
            throw new InvalidOperationException("宛先がありません。");

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidOperationException("件名がありません。");

        if (string.IsNullOrWhiteSpace(request.TextBody) &&
            string.IsNullOrWhiteSpace(request.HtmlBody))
            throw new InvalidOperationException("本文がありません。");
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