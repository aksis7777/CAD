using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Composition.Features.Queries.GetComposition;
using MiniPdm.Modules.Composition.Features.Queries.GetVersionComposition;
using MiniPdm.Modules.Composition.Features.Commands.ReplaceComposition;
using MiniPdm.Modules.Versions.Services;

namespace MiniPdm.Modules.Composition.Controllers;

/// <summary>
/// Предоставляет HTTP-операции чтения и изменения состава объектов.
/// Запросы направляются соответствующим обработчикам MediatR.
/// </summary>
/// <param name="sender">Посредник для отправки команд и запросов модулей.</param>
[ApiController]
[Route("api/objects")]
public sealed class CompositionController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Возвращает дерево состава указанного объекта.
    /// </summary>
    /// <param name="objectId">Идентификатор корневого объекта.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    /// <returns>Ответ с деревом состава, ошибкой запроса или статусом отсутствующего объекта.</returns>
    [HttpGet("{objectId:guid}/composition")]
    public async Task<IActionResult> Get(Guid objectId, CancellationToken cancellationToken)
    {
        if (objectId == Guid.Empty)
            return BadRequest("Object ID must not be empty.");

        var composition = await sender.Send(new GetCompositionQuery(objectId), cancellationToken);
        return composition is null ? NotFound() : Ok(composition);
    }

    /// <summary>
    /// Возвращает состав выбранной версии объекта.
    /// </summary>
    /// <param name="objectId">Идентификатор объекта.</param>
    /// <param name="version">Положительный номер версии.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    /// <returns>Ответ с составом версии, ошибкой запроса или статусом отсутствующей версии.</returns>
    [HttpGet("{objectId:guid}/versions/{version:int}/composition")]
    public async Task<IActionResult> GetVersion(Guid objectId, int version, CancellationToken cancellationToken)
    {
        if (objectId == Guid.Empty)
            return BadRequest("Object ID must not be empty.");
        if (version <= 0)
            return BadRequest("Version must be positive.");

        var composition = await sender.Send(new GetVersionCompositionQuery(objectId, version), cancellationToken);
        return composition is null ? NotFound() : Ok(composition);
    }

    /// <summary>
    /// Заменяет состав выбранной версии объекта.
    /// Переданное тело содержит дочерние объекты, количества и ожидаемый токен конкурентности.
    /// </summary>
    /// <param name="objectId">Идентификатор родительского объекта.</param>
    /// <param name="version">Положительный номер изменяемой версии.</param>
    /// <param name="request">Тело команды с новым составом и токеном конкурентности.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    /// <returns>Успешный результат мутации, диагностическая ошибка или ответ о неизвестном результате записи.</returns>
    [HttpPut("{objectId:guid}/versions/{version:int}/composition")]
    public async Task<IActionResult> Replace(Guid objectId, int version,
        [FromBody] ReplaceCompositionRequestDto? request, CancellationToken cancellationToken = default)
    {
        if (objectId == Guid.Empty)
            return BadRequest("Object ID must not be empty.");
        if (version <= 0)
            return BadRequest("Version must be positive.");
        if (request is null)
            return BadRequest("A request body is required.");
        if (request.Components is null)
            return BadRequest("Components must be provided.");
        if (request.ExpectedConcurrencyToken == Guid.Empty)
            return BadRequest("Expected concurrency token must not be empty.");
        if (request.Components.Any(x => x is null || x.ChildObjectId == Guid.Empty))
            return BadRequest("Each component must have a non-empty child object ID.");

        var components = request.Components.Select(x => new MiniPdm.Domain.Versions.Mutations.CompositionItem(x.ChildObjectId, x.Quantity)).ToArray();
        VersionMutationResult result;
        try
        {
            result = await sender.Send(new ReplaceCompositionCommand(objectId, version, components,
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
}
