using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;

namespace MiniPdm.Modules.Objects.Features.Queries.GetObject;

public sealed record GetObjectQuery(Guid ObjectId, int? VersionNumber) : IRequest<ObjectCardDto?>;
