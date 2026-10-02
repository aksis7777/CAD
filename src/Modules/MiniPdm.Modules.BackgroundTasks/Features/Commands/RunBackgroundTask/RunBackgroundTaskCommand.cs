using MediatR;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.Commands.RunBackgroundTask;

public sealed record RunBackgroundTaskCommand(string TaskId) : IRequest<BackgroundTaskRunRequestResult>;
