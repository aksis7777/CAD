namespace MiniPdm.Storage.Entities;

public sealed class BackgroundTask
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; }
    public string State { get; set; } = "Idle";
    public DateTimeOffset? NextRunAt { get; set; }
    public DateTimeOffset? LastStartedAt { get; set; }
    public DateTimeOffset? LastCompletedAt { get; set; }
    public string? LastResult { get; set; }
    public string? LastError { get; set; }
}
