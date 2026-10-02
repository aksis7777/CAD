using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Objects.Features.Queries.GetObject;
using MiniPdm.Modules.Objects.Features.Queries.SearchObjects;
using MiniPdm.Modules.Objects.Features.Commands.UpdateVersionAttributes;
using MiniPdm.Modules.Versions.Services;

namespace MiniPdm.Modules.Objects.Controllers;

[ApiController]
[Route("api/objects")]
public sealed class ObjectsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search = null, [FromQuery] int offset = 0, [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        search ??= string.Empty;
        if (search.Length > 512) return BadRequest("Search must be at most 512 characters.");
        if (offset < 0) return BadRequest("Offset must be nonnegative.");
        if (limit is < 1 or > 100) return BadRequest("Limit must be between 1 and 100.");

        return Ok(await sender.Send(new SearchObjectsQuery(search, offset, limit), cancellationToken));
    }

    [HttpGet("{objectId:guid}")]
    public async Task<IActionResult> Get(Guid objectId, [FromQuery] int? version = null, CancellationToken cancellationToken = default)
    {
        if (objectId == Guid.Empty) return BadRequest("Object ID must not be empty.");
        if (version is <= 0) return BadRequest("Version must be positive.");

        var result = await sender.Send(new GetObjectQuery(objectId, version), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{objectId:guid}/versions/{version:int}/attributes")]
    public async Task<IActionResult> UpdateVersionAttributes(Guid objectId, int version,
        [FromBody] UpdateVersionAttributesRequestDto? request, CancellationToken cancellationToken = default)
    {
        if (objectId == Guid.Empty) return BadRequest("Object ID must not be empty.");
        if (version <= 0) return BadRequest("Version must be positive.");
        if (request is null) return BadRequest("A request body is required.");
        if (request.ExpectedConcurrencyToken == Guid.Empty) return BadRequest("Expected concurrency token must not be empty.");

        VersionMutationResult result;
        try
        {
            result = await sender.Send(new UpdateVersionAttributesCommand(objectId, version, request.Name,
                request.Material, request.Mass, request.ExpectedConcurrencyToken), cancellationToken);
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
