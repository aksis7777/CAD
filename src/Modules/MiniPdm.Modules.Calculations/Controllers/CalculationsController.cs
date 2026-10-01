using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Modules.Calculations.Features.GetCompositionCalculation;

namespace MiniPdm.Modules.Calculations.Controllers;

[ApiController]
[Route("api/objects/{objectId:guid}/calculations")]
public sealed class CalculationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CompositionCalculationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompositionCalculationDto>> Get(Guid objectId, CancellationToken cancellationToken)
    {
        if (objectId == Guid.Empty) return BadRequest();

        var result = await sender.Send(new GetCompositionCalculationQuery(objectId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
