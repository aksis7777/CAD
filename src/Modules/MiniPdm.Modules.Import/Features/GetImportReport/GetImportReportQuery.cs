using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;

namespace MiniPdm.Modules.Import.Features.GetImportReport;

public sealed record GetImportReportQuery(Guid ImportId) : IRequest<ImportReportDto?>;
