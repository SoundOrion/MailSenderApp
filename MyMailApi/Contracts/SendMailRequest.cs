using MyMailApi.Domain;

namespace MyMailApi.Contracts;

public sealed class SendMailRequest
{
    public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();

    public string Subject { get; init; } = string.Empty;

    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }

    public MailPriorityLevel Priority { get; init; } = MailPriorityLevel.Normal;

    public IReadOnlyList<SendMailAttachmentDto> Attachments { get; init; } = Array.Empty<SendMailAttachmentDto>();

    // nullなら既定モードを使う
    public MailDispatchMode? Mode { get; init; }
}