namespace MyMailApi.Contracts;

public sealed class ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }
}