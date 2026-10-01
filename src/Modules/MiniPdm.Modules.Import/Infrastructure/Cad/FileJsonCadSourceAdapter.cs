using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.DtoModels.Cad;

namespace MiniPdm.Modules.Import.Infrastructure.Cad;

public sealed class FileJsonCadSourceAdapter : ICadSourceAdapter
{
    public const string SourceKind = "file-json";
    public string Kind => SourceKind;

    public Task<ICadSession> OpenAsync(CadSourceDescriptor descriptor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(descriptor.Kind, Kind, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Source kind '{descriptor.Kind}' is not supported by this adapter.", nameof(descriptor));
        if (string.IsNullOrWhiteSpace(descriptor.Location))
            throw new ArgumentException("A source directory is required.", nameof(descriptor));

        var fullPath = Path.GetFullPath(descriptor.Location);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"CAD source directory was not found: {fullPath}");

        ICadSession session = new FileJsonCadSession(fullPath);
        return Task.FromResult(session);
    }
}

internal sealed class FileJsonCadSession(string directory) : ICadSession
{
    public ICadDocumentSource Source { get; } = new FileJsonCadDocumentSource(directory);
    public ICadDocumentReader Reader { get; } = new FileJsonCadDocumentReader(directory);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
