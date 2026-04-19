namespace MailSenderApp.Models;

public sealed class MailQueueOptions
{
    public const string SectionName = "MailQueue";

    public int Capacity { get; set; } = 100;
    public int MaxRetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}