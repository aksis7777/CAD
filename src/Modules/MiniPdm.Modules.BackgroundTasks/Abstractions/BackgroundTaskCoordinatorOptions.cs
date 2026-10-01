namespace MiniPdm.Modules.BackgroundTasks.Abstractions;

public sealed record BackgroundTaskCoordinatorOptions(TimeSpan CompletionWriteRetryDelay)
{
    public static BackgroundTaskCoordinatorOptions Default { get; } = new(TimeSpan.FromSeconds(30));
}
