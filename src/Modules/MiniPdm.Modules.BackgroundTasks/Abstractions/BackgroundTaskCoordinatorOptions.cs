namespace MiniPdm.Modules.BackgroundTasks.Abstractions;

/// <summary>
/// Настраивает поведение координатора фоновых задач.
/// </summary>
/// <param name="CompletionWriteRetryDelay">Задержка перед повторной записью результата завершённой задачи.</param>
public sealed record BackgroundTaskCoordinatorOptions(TimeSpan CompletionWriteRetryDelay)
{
    /// <summary>
    /// Параметры координатора по умолчанию с задержкой повтора записи в 30 секунд.
    /// </summary>
    public static BackgroundTaskCoordinatorOptions Default { get; } = new(TimeSpan.FromSeconds(30));
}
