using System.ComponentModel.DataAnnotations;

namespace MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

public sealed record BackgroundTaskDto(
    string Id,
    string Name,
    int IntervalMinutes,
    string State,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastResult,
    string? LastError);

public sealed record UpdateBackgroundTaskScheduleRequestDto([Range(1, 525600)] int IntervalMinutes);

public sealed record BackgroundTaskRunAcceptedDto(string TaskId);
