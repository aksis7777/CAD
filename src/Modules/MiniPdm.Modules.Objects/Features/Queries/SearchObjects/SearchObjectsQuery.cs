using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;

namespace MiniPdm.Modules.Objects.Features.Queries.SearchObjects;

public sealed record SearchObjectsQuery(string Search, int Offset, int Limit) : IRequest<ObjectSearchPageDto>;
