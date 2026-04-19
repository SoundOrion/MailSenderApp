namespace MyMailApi.Contracts;

public sealed class SendMailResponse
{
    public string Message { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
}