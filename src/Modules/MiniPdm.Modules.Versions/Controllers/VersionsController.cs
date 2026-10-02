using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Domain.Versions;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Features.Commands.ChangeVersionState;
using MiniPdm.Modules.Versions.Features.Commands.CloneVersion;
using MiniPdm.Modules.Versions.Services;

namespace MiniPdm.Modules.Versions.Controllers;

/// <summary>
/// Предоставляет HTTP-операции создания версий и изменения их состояния.
/// </summary>
/// <param name="sender">Посредник команд приложения.</param>
[ApiController]
[Route("api/objects")]
public sealed class VersionsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Создаёт версию, клонируя указанную исходную версию объекта.
    /// </summary>
    /// <param name="objectId">Идентификатор объекта в маршруте.</param>
    /// <param name="request">Исходная версия и ожидаемый токен объекта.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP-ответ с созданной версией либо описанием ошибки проверки или конфликта.</returns>
    [HttpPost("{objectId:guid}/versions")]
    public async Task<IActionResult> Clone(Guid objectId, [FromBody] CloneVersionRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        if (objectId == Guid.Empty)
            return BadRequest("Object ID must not be empty.");
        if (request is null)
            return BadRequest("A request body is required.");
        if (request.SourceVersion <= 0)
            return BadRequest("Source version must be positive.");
        if (request.ExpectedConcurrencyToken == Guid.Empty)
            return BadRequest("Expected concurrency token must not be empty.");

        VersionMutationResult result;
        try
        {
            result = await sender.Send(new CloneVersionCommand(objectId, request.SourceVersion,
                request.ExpectedConcurrencyToken), cancellationToken);
        }
        catch (VersionWriteUncertainException)
        {
            return UncertainWrite();
        }

        var response = Map(result);
        if (response is not null)
            return response;
        var dto = ToDto(result);
        return Created($"/api/objects/{result.ObjectId}?version={result.VersionNumber}", dto);
    }

    /// <summary>
    /// Меняет состояние заданной версии объекта.
    /// </summary>
    /// <param name="objectId">Идентификатор объекта в маршруте.</param>
    /// <param name="version">Номер версии в маршруте.</param>
    /// <param name="request">Новое состояние и ожидаемый токен объекта.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>HTTP-ответ с результатом операции или описанием ошибки.</returns>
    [HttpPut("{objectId:guid}/versions/{version:int}/state")]
    public async Task<IActionResult> ChangeState(Guid objectId, int version,
        [FromBody] ChangeVersionStateRequestDto? request, CancellationToken cancellationToken = default)
    {
        if (objectId == Guid.Empty)
            return BadRequest("Object ID must not be empty.");
        if (version <= 0)
            return BadRequest("Version must be positive.");
        if (request is null)
            return BadRequest("A request body is required.");
        if (request.ExpectedConcurrencyToken == Guid.Empty)
            return BadRequest("Expected concurrency token must not be empty.");
        if (!TryParseState(request.State, out var state))
            return BadRequest("State must be InWork, Approved, or Cancelled.");

        VersionMutationResult result;
        try
        {
            result = await sender.Send(new ChangeVersionStateCommand(objectId, version, state,
                request.ExpectedConcurrencyToken), cancellationToken);
        }
        catch (VersionWriteUncertainException)
        {
            return UncertainWrite();
        }

        var response = Map(result);
        return response ?? Ok(ToDto(result));
    }

    private IActionResult? Map(VersionMutationResult result) => result.Status switch
    {
        VersionMutationStatus.Succeeded => null,
        VersionMutationStatus.NotFound => NotFound(ToErrorDto(result.Error)),
        VersionMutationStatus.Conflict => Conflict(ToErrorDto(result.Error)),
        VersionMutationStatus.Invalid => BadRequest(ToErrorDto(result.Error)),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };

    private static VersionMutationDto ToDto(VersionMutationResult result) => new(
        result.ObjectId,
        result.VersionId!.Value,
        result.VersionNumber!.Value,
        result.State!.Value.ToString(),
        result.CurrentVersionId,
        result.ConcurrencyToken!.Value,
        result.Warnings);

    private static VersionMutationErrorDto ToErrorDto(VersionMutationError? error) => error is null
        ? new("VersionMutationFailed", "The version mutation could not be completed.", null)
        : new(error.Code, error.Message, error.CyclePath);

    private ObjectResult UncertainWrite() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "The version write outcome is unknown.",
        detail: "Read the object card to determine whether the change was committed before retrying.");

    private static bool TryParseState(string? value, out VersionState state)
    {
        state = default;
        return value is not null && Enum.GetNames<VersionState>().Any(name =>
            string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) &&
            Enum.TryParse(value, ignoreCase: true, out state) && Enum.IsDefined(state);
    }
}
