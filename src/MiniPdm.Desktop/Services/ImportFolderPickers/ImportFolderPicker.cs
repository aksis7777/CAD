using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MiniPdm.Desktop.Services.ImportFolderPickers;

public sealed class ImportFolderPicker : IImportFolderPicker
{
    private readonly BrowserImportFolderPicker? _browser;

    public ImportFolderPicker(BrowserImportFolderPicker? browser = null) => _browser = browser;

    public async Task<SelectedImportPackage?> PickAsync(Window owner, CancellationToken cancellationToken = default)
    {
        if (_browser is null) return await PickNativeAsync(owner, cancellationToken);
        return await _browser.PickAsync(cancellationToken);
    }

    private static async Task<SelectedImportPackage?> PickNativeAsync(Window owner, CancellationToken cancellationToken)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку CAD-документов",
            AllowMultiple = false
        });
        cancellationToken.ThrowIfCancellationRequested();
        var folder = folders.FirstOrDefault();
        if (folder is null) return null;

        var paths = new List<string>();
        await foreach (var item in folder.GetItemsAsync().WithCancellation(cancellationToken))
        {
            if (item is not IStorageFile || item.TryGetLocalPath() is not { } path) continue;
            var extension = Path.GetExtension(path);
            if (extension.Equals(".a3d", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".m3d", StringComparison.OrdinalIgnoreCase))
                paths.Add(path);
        }

        return new SelectedImportPackage(paths
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }
}
