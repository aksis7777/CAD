using MiniPdm.Modules.BackgroundTasks.DtoModels;

namespace MiniPdm.Modules.BackgroundTasks.Abstractions.Database;

/// <summary>Database operations used by the BackgroundTasks module.</summary>
public interface IBackgroundTaskDatabaseService
{
    Task EnsureDefinitionsAsync(IReadOnlyCollection<BackgroundTaskDefinitionRecord> definitions, DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<BackgroundTaskRow>> ListAsync(CancellationToken ct);
    Task<BackgroundTaskRow?> GetAsync(string id, CancellationToken ct);
    Task<bool> UpdateScheduleAsync(string id, int intervalMinutes, DateTimeOffset now, CancellationToken ct);
    Task<bool> TryStartAsync(string id, DateTimeOffset startedAt, CancellationToken ct);
    Task<bool> CompleteAsync(string id, string state, DateTimeOffset completedAt, string? result, string? error, CancellationToken ct);
}
