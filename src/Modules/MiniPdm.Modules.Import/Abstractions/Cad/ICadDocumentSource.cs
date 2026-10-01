using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions.Cad;

public interface ICadDocumentSource
{
    IAsyncEnumerable<CadDocumentRef> GetDocumentsAsync(CancellationToken cancellationToken);
}
