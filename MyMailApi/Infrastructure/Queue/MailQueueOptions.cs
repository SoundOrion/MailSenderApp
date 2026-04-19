namespace MyMailApi.Infrastructure.Queue;

public sealed class MailQueueOptions
{
    public const string SectionName = "MailQueue";

    public int Capacity { get; set; } = 200;
    public int MaxRetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}