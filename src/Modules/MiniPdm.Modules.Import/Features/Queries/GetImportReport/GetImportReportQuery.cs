using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;

namespace MiniPdm.Modules.Import.Features.Queries.GetImportReport;

public sealed record GetImportReportQuery(Guid ImportId) : IRequest<ImportReportDto?>;
