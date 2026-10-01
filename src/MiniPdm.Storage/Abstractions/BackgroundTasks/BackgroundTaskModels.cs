namespace MiniPdm.Storage.Abstractions.BackgroundTasks;

public sealed record BackgroundTaskDefinitionRecord(string Id, string Name, int DefaultIntervalMinutes);

public sealed record BackgroundTaskRow(
    string Id,
    string Name,
    int IntervalMinutes,
    string State,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastResult,
    string? LastError);

public interface IBackgroundTaskPersistence
{
    Task EnsureDefinitionsAsync(IReadOnlyCollection<BackgroundTaskDefinitionRecord> definitions, DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<BackgroundTaskRow>> ListAsync(CancellationToken ct);
    Task<BackgroundTaskRow?> GetAsync(string id, CancellationToken ct);
    Task<bool> UpdateScheduleAsync(string id, int intervalMinutes, DateTimeOffset now, CancellationToken ct);
    Task<bool> TryStartAsync(string id, DateTimeOffset startedAt, CancellationToken ct);
    Task<bool> CompleteAsync(string id, string state, DateTimeOffset completedAt, string? result, string? error, CancellationToken ct);
}
