using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions.Cad;

public interface ICadSourceAdapter
{
    string Kind { get; }
    Task<ICadSession> OpenAsync(CadSourceDescriptor descriptor, CancellationToken cancellationToken);
}
