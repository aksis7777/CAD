using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

namespace MiniPdm.Modules.BackgroundTasks.Features.ListBackgroundTasks;

public sealed record ListBackgroundTasksQuery : IRequest<IReadOnlyList<BackgroundTaskDto>>;
