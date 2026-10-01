using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MiniPdm.Desktop.ViewModels;

namespace MiniPdm.Desktop;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void PickImportFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выберите папку CAD-документов",
                AllowMultiple = false
            });
            var path = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path)) return;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Where(file => Path.GetExtension(file).Equals(".a3d", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(file).Equals(".m3d", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                .ToArray();
            await viewModel.ImportFolderAsync(files);
        }
        catch (Exception ex) { viewModel.ReportError($"Не удалось прочитать выбранную папку: {ex.Message}"); }
    }

    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RefreshCommand.Execute(null);
            e.Handled = true;
        }
    }
}
