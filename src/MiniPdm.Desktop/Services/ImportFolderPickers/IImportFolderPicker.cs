using Avalonia.Controls;

namespace MiniPdm.Desktop.Services.ImportFolderPickers;

public interface IImportFolderPicker
{
    Task<SelectedImportPackage?> PickAsync(Window owner, CancellationToken cancellationToken = default);
}
