namespace MailSenderApp.Models;

public sealed class MailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }

    public byte[]? Data { get; init; }
    public Stream? ContentStream { get; init; }
    public string? FilePath { get; init; }

    public static MailAttachment FromBytes(
        string fileName,
        byte[] data,
        string? contentType = null)
    {
        return new MailAttachment
        {
            FileName = fileName,
            Data = data,
            ContentType = contentType
        };
    }

    public static MailAttachment FromStream(
        string fileName,
        Stream contentStream,
        string? contentType = null)
    {
        return new MailAttachment
        {
            FileName = fileName,
            ContentStream = contentStream,
            ContentType = contentType
        };
    }

    public static MailAttachment FromFile(
        string filePath,
        string? fileName = null,
        string? contentType = null)
    {
        return new MailAttachment
        {
            FilePath = filePath,
            FileName = fileName ?? Path.GetFileName(filePath),
            ContentType = contentType
        };
    }

    public void Validate()
    {
        var sourceCount =
            (Data is not null ? 1 : 0) +
            (ContentStream is not null ? 1 : 0) +
            (!string.IsNullOrWhiteSpace(FilePath) ? 1 : 0);

        if (sourceCount == 0)
            throw new InvalidOperationException($"添付 '{FileName}' にデータソースがありません。");

        if (sourceCount > 1)
            throw new InvalidOperationException($"添付 '{FileName}' に複数ソースが指定されています。");
    }
}