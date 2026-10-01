using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Modules.BackgroundTasks.Abstractions;
using MiniPdm.Modules.BackgroundTasks.Features.ListBackgroundTasks;
using MiniPdm.Modules.BackgroundTasks.Features.RunBackgroundTask;
using MiniPdm.Modules.BackgroundTasks.Features.UpdateBackgroundTaskSchedule;

namespace MiniPdm.Modules.BackgroundTasks.Controllers;

[ApiController]
[Route("api/background-tasks")]
public sealed class BackgroundTasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BackgroundTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try { return Ok(await sender.Send(new ListBackgroundTasksQuery(), cancellationToken)); }
        catch (BackgroundTaskPersistenceUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message }); }
    }

    [HttpPut("{taskId}/schedule")]
    [ProducesResponseType(typeof(BackgroundTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateSchedule(string taskId, UpdateBackgroundTaskScheduleRequestDto request, CancellationToken cancellationToken)
    {
        if (request.IntervalMinutes is < 1 or > 525600)
            return BadRequest(new { error = "IntervalMinutes must be between 1 and 525600." });
        try
        {
            var result = await sender.Send(new UpdateBackgroundTaskScheduleCommand(taskId, request.IntervalMinutes), cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (BackgroundTaskPersistenceUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message }); }
    }

    [HttpPost("{taskId}/run")]
    [ProducesResponseType(typeof(BackgroundTaskRunAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Run(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new RunBackgroundTaskCommand(taskId), cancellationToken);
            return result.Status switch
            {
                BackgroundTaskRunRequestStatus.Accepted => Accepted(value: new BackgroundTaskRunAcceptedDto(taskId)),
                BackgroundTaskRunRequestStatus.Running => Conflict(new { error = "The background task is already running." }),
                _ => NotFound()
            };
        }
        catch (BackgroundTaskPersistenceUnavailableException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message }); }
    }
}
