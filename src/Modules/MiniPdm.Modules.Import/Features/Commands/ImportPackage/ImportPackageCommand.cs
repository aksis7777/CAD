using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Features.Commands.ImportPackage;

public sealed record ImportPackageCommand(Guid ImportId, CadSourceDescriptor Source) : IRequest<ImportReportDto>;
