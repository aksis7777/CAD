namespace MiniPdm.Modules.BackgroundTasks.DtoModels;

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
