using Avalonia.Controls;
using Avalonia;
using MiniPdm.Desktop.Modules.BackgroundTasks.ViewModels;

namespace MiniPdm.Desktop.Modules.BackgroundTasks.Views;

public partial class BackgroundTasksView : UserControl
{
    public BackgroundTasksView() => InitializeComponent();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is BackgroundTasksViewModel viewModel) viewModel.CancelActivePolling();
        base.OnDetachedFromVisualTree(e);
    }
}
