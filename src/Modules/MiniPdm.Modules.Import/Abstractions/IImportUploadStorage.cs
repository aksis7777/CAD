using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions;

public sealed record ImportUploadFile(string FileName, Stream Content);

public interface IImportUploadAttempt : IAsyncDisposable
{
    CadSourceDescriptor SourceDescriptor { get; }
}

public interface IImportUploadStorage
{
    Task<IImportUploadAttempt> StageAsync(Guid importId, IReadOnlyList<ImportUploadFile> files, CancellationToken ct);
}
