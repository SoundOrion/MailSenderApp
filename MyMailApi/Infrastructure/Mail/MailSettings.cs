namespace MyMailApi.Infrastructure.Mail;

public sealed class MailSettings
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TimeoutMilliseconds { get; set; } = 30000;
}