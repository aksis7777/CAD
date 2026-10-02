using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;
using MiniPdm.Modules.Import.Services;

namespace MiniPdm.Modules.Import.Features.Commands.ImportPackage;

public sealed record ImportPackageCommand(Guid ImportId, CadSourceDescriptor Source) : IRequest<ImportReportDto>;

public sealed class ImportPackageCommandHandler(ImportService service) : IRequestHandler<ImportPackageCommand, ImportReportDto>
{
    public Task<ImportReportDto> Handle(ImportPackageCommand request, CancellationToken cancellationToken) =>
        service.ExecuteAsync(request.ImportId, request.Source, cancellationToken);
}
