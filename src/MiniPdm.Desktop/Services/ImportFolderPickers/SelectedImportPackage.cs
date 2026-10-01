namespace MiniPdm.Desktop.Services.ImportFolderPickers;

/// <summary>A selected CAD packet whose files remain available until import is confirmed or abandoned.</summary>
public sealed class SelectedImportPackage : IDisposable
{
    private IDisposable? _lease;

    public SelectedImportPackage(IReadOnlyList<string> filePaths, IDisposable? lease = null)
    {
        FilePaths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
        _lease = lease;
    }

    public IReadOnlyList<string> FilePaths { get; }
    public void Dispose() => Interlocked.Exchange(ref _lease, null)?.Dispose();
}
