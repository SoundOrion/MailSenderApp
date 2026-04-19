namespace MyMailApi.Contracts;

public sealed class SendMailAttachmentDto
{
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }

    // Base64文字列で受ける
    public string Base64Data { get; init; } = string.Empty;
}