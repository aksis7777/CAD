using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions.Cad;

public interface ICadDocumentReader
{
    Task<CadReadResult> ReadAsync(CadDocumentRef document, CancellationToken cancellationToken);
}
