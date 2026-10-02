using MediatR;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;
using MiniPdm.Modules.Import.Services;

namespace MiniPdm.Modules.Import.Features.Commands.ImportPackage;

/// <summary>
/// Запрашивает чтение, проверку и сохранение пакета CAD-документов.
/// </summary>
/// <param name="ImportId">Идентификатор импорта для идемпотентной обработки.</param>
/// <param name="Source">Описатель источника пакета.</param>
public sealed record ImportPackageCommand(Guid ImportId, CadSourceDescriptor Source) : IRequest<ImportReportDto>;

/// <summary>
/// Передаёт команду импорта прикладному сервису.
/// </summary>
/// <param name="service">Сервис чтения и сохранения импорта.</param>
public sealed class ImportPackageCommandHandler(ImportService service) : IRequestHandler<ImportPackageCommand, ImportReportDto>
{
    /// <summary>
    /// Выполняет импорт указанного источника.
    /// </summary>
    /// <param name="request">Команда с идентификатором импорта и источником.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача с отчётом о результатах обработки файлов.</returns>
    public Task<ImportReportDto> Handle(ImportPackageCommand request, CancellationToken cancellationToken) =>
        service.ExecuteAsync(request.ImportId, request.Source, cancellationToken);
}
