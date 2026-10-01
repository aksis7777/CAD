using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;

namespace MiniPdm.Desktop.Services.Abstractions;

public interface IPdmApiClient
{
    Task<ObjectSearchPageDto> SearchObjectsAsync(string? search = null, int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default);

    Task<ObjectCardDto> GetObjectAsync(Guid objectId, int? version = null,
        CancellationToken cancellationToken = default);

    Task<CompositionTreeDto> GetCompositionAsync(Guid objectId, CancellationToken cancellationToken = default);

    Task<VersionCompositionDto> GetVersionCompositionAsync(Guid objectId, int version,
        CancellationToken cancellationToken = default);

    Task<CompositionCalculationDto> GetCalculationAsync(Guid objectId, CancellationToken cancellationToken = default);

    Task<ImportReportDto> ImportFilesAsync(Guid importId, IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default);

    Task<ImportReportDto> GetImportReportAsync(Guid importId, CancellationToken cancellationToken = default);

    Task<VersionMutationDto> CloneVersionAsync(Guid objectId, CloneVersionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<VersionMutationDto> ChangeVersionStateAsync(Guid objectId, int version,
        ChangeVersionStateRequestDto request, CancellationToken cancellationToken = default);

    Task<VersionMutationDto> UpdateVersionAttributesAsync(Guid objectId, int version,
        UpdateVersionAttributesRequestDto request, CancellationToken cancellationToken = default);

    Task<VersionMutationDto> ReplaceCompositionAsync(Guid objectId, int version,
        ReplaceCompositionRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackgroundTaskDto>> GetBackgroundTasksAsync(CancellationToken cancellationToken = default);

    Task<BackgroundTaskDto> UpdateBackgroundTaskScheduleAsync(string taskId,
        UpdateBackgroundTaskScheduleRequestDto request, CancellationToken cancellationToken = default);

    Task<BackgroundTaskRunAcceptedDto> RunBackgroundTaskAsync(string taskId,
        CancellationToken cancellationToken = default);
}
