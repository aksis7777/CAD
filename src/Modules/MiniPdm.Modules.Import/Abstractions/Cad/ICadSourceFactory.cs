using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Abstractions.Cad;

public interface ICadSourceFactory
{
    Task<ICadSession> OpenAsync(CadSourceDescriptor descriptor, CancellationToken cancellationToken);
}
