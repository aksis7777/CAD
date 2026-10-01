using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions;

public interface IImportSourceStorage
{
    Task PromoteAsync(Guid importId, CadSourceDescriptor source, IReadOnlyCollection<string> acceptedFiles, CancellationToken ct);
    string GetSourceReference(Guid importId, string fileName);
    Task CompensateAsync(Guid importId, CancellationToken ct);
}
