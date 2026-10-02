using MediatR;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;

namespace MiniPdm.Modules.Calculations.Features.Queries.GetCompositionCalculation;

public sealed record GetCompositionCalculationQuery(Guid ObjectId) : IRequest<CompositionCalculationDto?>;
