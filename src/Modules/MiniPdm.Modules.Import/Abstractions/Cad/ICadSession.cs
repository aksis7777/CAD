namespace MiniPdm.Modules.Import.Abstractions.Cad;

public interface ICadSession : IAsyncDisposable
{
    ICadDocumentSource Source { get; }
    ICadDocumentReader Reader { get; }
}
