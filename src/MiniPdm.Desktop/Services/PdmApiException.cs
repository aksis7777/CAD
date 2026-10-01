namespace MiniPdm.Desktop.Services;

public sealed class PdmApiException : Exception
{
    public PdmApiException(int statusCode, string message, string? errorCode = null,
        IReadOnlyList<Guid>? cyclePath = null, string? responseBody = null)
        : base(FormatMessage(message, cyclePath))
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        CyclePath = cyclePath;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }
    public string? ErrorCode { get; }
    public IReadOnlyList<Guid>? CyclePath { get; }
    public string? ResponseBody { get; }
    public bool IsOutcomeUnknown => StatusCode == 503;

    private static string FormatMessage(string message, IReadOnlyList<Guid>? cyclePath)
    {
        if (cyclePath is not { Count: > 0 }) return message;
        return $"{message} Cycle: {string.Join(" → ", cyclePath)}";
    }
}
