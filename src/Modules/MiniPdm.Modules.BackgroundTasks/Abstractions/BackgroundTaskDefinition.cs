namespace MiniPdm.Modules.BackgroundTasks.Abstractions;

public sealed record BackgroundTaskExecutionResult(string Summary, IReadOnlyList<string> Errors);

public sealed record BackgroundTaskDefinition(
    string Id,
    string Name,
    int DefaultIntervalMinutes,
    Func<IServiceProvider, CancellationToken, Task<BackgroundTaskExecutionResult>> ExecuteAsync);

public enum BackgroundTaskRunRequestStatus { Accepted, NotFound, Running }

public sealed record BackgroundTaskRunRequestResult(BackgroundTaskRunRequestStatus Status);

public interface IBackgroundTaskCoordinator
{
    Task<IReadOnlyList<MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels.BackgroundTaskDto>> ListAsync(CancellationToken ct);
    Task<MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels.BackgroundTaskDto?> UpdateScheduleAsync(string taskId, int intervalMinutes, CancellationToken ct);
    Task<BackgroundTaskRunRequestResult> RequestManualRunAsync(string taskId, CancellationToken ct);
}

public sealed class BackgroundTaskPersistenceUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
