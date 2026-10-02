using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

namespace MiniPdm.Modules.BackgroundTasks.Features.Commands.UpdateBackgroundTaskSchedule;

public sealed record UpdateBackgroundTaskScheduleCommand(string TaskId, int IntervalMinutes) : IRequest<BackgroundTaskDto?>;
