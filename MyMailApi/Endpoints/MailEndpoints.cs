using MyMailApi.Application.Abstractions;
using MyMailApi.Contracts;
using MyMailApi.Domain;
using MyMailApi.Infrastructure.Mail;
using Microsoft.Extensions.Options;

namespace MyMailApi.Endpoints;

public static class MailEndpoints
{
    public static IEndpointRouteBuilder MapMailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mail");

        group.MapPost("/send", SendMailAsync)
            .WithName("SendMail")
            .WithSummary("メール送信")
            .WithDescription("メールを即送信またはキュー送信します。");

        return app;
    }

    private static async Task<IResult> SendMailAsync(
        SendMailRequest request,
        IMailSender mailSender,
        IMailQueue mailQueue,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidateRequest(request);

            var message = MapToDomain(request);

            var defaultModeText = configuration["MailDispatch:DefaultMode"] ?? "Direct";
            var defaultMode = Enum.TryParse<MailDispatchMode>(defaultModeText, ignoreCase: true, out var parsedMode)
                ? parsedMode
                : MailDispatchMode.Direct;

            var mode = request.Mode ?? defaultMode;

            switch (mode)
            {
                case MailDispatchMode.Queued:
                    EnsureQueueSafe(message);
                    await mailQueue.EnqueueAsync(message, cancellationToken);
                    return Results.Accepted(
                        value: new
                        {
                            message = "Mail queued.",
                            mode = mode.ToString()
                        });

                default:
                    await mailSender.SendAsync(message, cancellationToken);
                    return Results.Ok(new
                    {
                        message = "Mail sent.",
                        mode = mode.ToString()
                    });
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new
            {
                error = ex.Message
            });
        }
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

        foreach (var a in request.Attachments)
        {
            if (string.IsNullOrWhiteSpace(a.FileName))
            {
                throw new InvalidOperationException("添付ファイル名がありません。");
            }

            if (string.IsNullOrWhiteSpace(a.Base64Data))
            {
                throw new InvalidOperationException($"添付 '{a.FileName}' の Base64Data がありません。");
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
                    "キュー送信では Stream 添付は使えません。byte[] かファイル保存方式を使ってください。");
            }
        }
    }
}