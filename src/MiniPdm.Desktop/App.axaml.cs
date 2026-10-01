using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.ViewModels;

namespace MiniPdm.Desktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = PdmApiClientOptions.FromEnvironment();
            var httpClient = new HttpClient
            {
                BaseAddress = options.BaseAddress,
                Timeout = TimeSpan.FromMinutes(5)
            };
            IPdmApiClient client = new PdmApiClient(httpClient);
            var mainWindow = new MainWindow { DataContext = new MainWindowViewModel(client) };
            mainWindow.Closed += (_, _) => (mainWindow.DataContext as IDisposable)?.Dispose();
            desktop.Exit += (_, _) => httpClient.Dispose();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
