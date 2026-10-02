using MediatR;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Modules.Calculations.Services;

namespace MiniPdm.Modules.Calculations.Features.Queries.GetCompositionCalculation;

public sealed record GetCompositionCalculationQuery(Guid ObjectId) : IRequest<CompositionCalculationDto?>;

public sealed class GetCompositionCalculationQueryHandler(CompositionCalculationService service)
    : IRequestHandler<GetCompositionCalculationQuery, CompositionCalculationDto?>
{
    public Task<CompositionCalculationDto?> Handle(GetCompositionCalculationQuery request,
        CancellationToken cancellationToken) => service.GetCalculationAsync(request.ObjectId, cancellationToken);
}
