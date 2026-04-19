namespace MailSenderApp.Models;

public sealed class MailRequest
{
    public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();

    public string Subject { get; init; } = string.Empty;

    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }

    public MailPriorityLevel Priority { get; init; } = MailPriorityLevel.Normal;

    public IReadOnlyList<MailAttachment> Attachments { get; init; } = Array.Empty<MailAttachment>();
}