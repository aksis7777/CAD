using System.Net;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Desktop;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.ImportFolderPickers;
using MiniPdm.Desktop.ViewModels;
using Xunit;

namespace MiniPdm.Desktop.Tests;

public sealed class DesktopWindowTests
{
    private static readonly Guid AssemblyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid HistoricalChildId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid CurrentChildId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid CurrentVersionId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private static readonly Guid HistoricalVersionId = Guid.Parse("50000000-0000-0000-0000-000000000005");
    private static readonly Guid ConcurrencyToken = Guid.Parse("60000000-0000-0000-0000-000000000006");

    [AvaloniaFact]
    public async Task ImportReportContentUsesImportViewModelAsDataContext()
    {
        var handler = new DesktopApiHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        using var viewModel = new MainWindowViewModel(new PdmApiClient(httpClient));
        var window = new MainWindow { DataContext = viewModel, Width = 1480, Height = 920 };

        try
        {
            window.Show();
            await WaitUntilAsync(() => viewModel.Objects.Count == 3 && !viewModel.IsBusy,
                "Object search did not finish in time.");
            var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
            var reportTab = window.FindControl<TabItem>("ImportReportTab")!;
            tabs.SelectedItem = reportTab;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Same(viewModel.Import, window.FindControl<Grid>("ImportReportContent")!.DataContext);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task CancelingFolderPickerRestoresImportButtonAndClearsPickingState()
    {
        var handler = new DesktopApiHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        using var viewModel = new MainWindowViewModel(new PdmApiClient(httpClient));
        var picker = new ControlledFolderPicker();
        var window = new MainWindow { DataContext = viewModel, FolderPicker = picker, Width = 1480, Height = 920 };

        try
        {
            window.Show();
            await WaitUntilAsync(() => viewModel.Objects.Count == 3 && !viewModel.IsBusy,
                "Object search did not finish in time.");
            var importButton = window.FindControl<Button>("ImportFolderButton")!;
            Assert.True(importButton.IsEnabled);

            importButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await picker.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(importButton.IsEnabled);
            Assert.True(GetIsPicking(window));

            picker.Complete(null);
            await WaitUntilAsync(() => !GetIsPicking(window) && importButton.IsEnabled,
                "Folder picker cancellation did not restore the import button.");

            Assert.False(viewModel.Import.IsBusy);
            Assert.Null(viewModel.Import.PendingImportId);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SuccessfulRepeatedImportsRestoreButtonAndRenderEachReport()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-pdm-{Guid.NewGuid():N}.a3d");
        await File.WriteAllTextAsync(path, "{}");
        var handler = new DesktopApiHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        using var viewModel = new MainWindowViewModel(new PdmApiClient(httpClient));
        var picker = new RepeatingFolderPicker(path);
        var window = new MainWindow { DataContext = viewModel, FolderPicker = picker, Width = 1480, Height = 920 };

        try
        {
            window.Show();
            await WaitUntilAsync(() => viewModel.Objects.Count == 3 && !viewModel.IsBusy,
                "Object search did not finish in time.");
            var importButton = window.FindControl<Button>("ImportFolderButton")!;

            for (var importNumber = 1; importNumber <= 2; importNumber++)
            {
                Assert.True(importButton.IsEnabled);
                importButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await WaitUntilAsync(() => picker.CallCount == importNumber && !GetIsPicking(window)
                    && !viewModel.Import.IsBusy && viewModel.Import.PendingImportId is null
                    && viewModel.Import.Files.Count == 1 && importButton.IsEnabled,
                    $"Import {importNumber} did not finish and restore the import button.");

                Assert.Equal(importNumber, handler.ImportIds.Count);
                Assert.Single(viewModel.Import.Files);
                Assert.Same(viewModel.Import, window.FindControl<Grid>("ImportReportContent")!.DataContext);
                Assert.Same(viewModel.Import.Files, window.FindControl<ListBox>("ImportReportFiles")!.ItemsSource);
                Assert.Contains($"Принято: 1", viewModel.Import.StatusText);
                Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == viewModel.Import.StatusText);
            }
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public async Task UncertainImportOutcomeKeepsRetryVisibleAndRecoversWithTheSameId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-pdm-{Guid.NewGuid():N}.a3d");
        await File.WriteAllTextAsync(path, "{}");
        var handler = new DesktopApiHandler { ImportFailuresRemaining = 1 };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        using var viewModel = new MainWindowViewModel(new PdmApiClient(httpClient));
        var picker = new RepeatingFolderPicker(path);
        var window = new MainWindow { DataContext = viewModel, FolderPicker = picker, Width = 1480, Height = 920 };

        try
        {
            window.Show();
            await WaitUntilAsync(() => viewModel.Objects.Count == 3 && !viewModel.IsBusy,
                "Object search did not finish in time.");
            var importButton = window.FindControl<Button>("ImportFolderButton")!;
            importButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => !GetIsPicking(window) && !viewModel.Import.IsBusy
                && viewModel.Import.PendingImportId.HasValue,
                "The simulated uncertain import outcome did not remain pending.");

            var pendingId = viewModel.Import.PendingImportId;
            var retryButton = window.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "Проверить / повторить с тем же ID");
            var abandonButton = window.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "Отказаться от повтора этого пакета");
            Assert.True(retryButton.IsVisible);
            Assert.True(retryButton.IsEnabled);
            Assert.True(abandonButton.IsVisible);
            Assert.True(abandonButton.IsEnabled);
            Assert.False(importButton.IsEnabled);
            Assert.False(viewModel.Import.IsBusy);

            await viewModel.Import.RetryCommand.ExecuteAsync();
            await WaitUntilAsync(() => !viewModel.Import.IsBusy && viewModel.Import.PendingImportId is null
                && importButton.IsEnabled && viewModel.Import.Files.Count == 1,
                "Retry did not confirm the import and restore the button.");

            Assert.Equal(new Guid?[] { pendingId, pendingId }, handler.ImportIds.Select(id => (Guid?)id));
            Assert.Single(viewModel.Import.Files);
            Assert.Contains("Принято: 1", viewModel.Import.StatusText);
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public async Task WindowLoadsHistoricalEditableBomBesideCurrentTreeAndCalculation()
    {
        var previousLogSink = Logger.Sink;
        var bindingErrors = new BindingErrorCollector();
        Logger.Sink = bindingErrors;
        var handler = new DesktopApiHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        var viewModel = new MainWindowViewModel(new PdmApiClient(httpClient));
        var window = new MainWindow { DataContext = viewModel, Width = 1480, Height = 920 };

        try
        {
            window.Show();
            await WaitUntilAsync(() => viewModel.Objects.Count == 3 && !viewModel.IsBusy,
                "Object search did not finish in time.");

            viewModel.SelectedObject = viewModel.Objects.Single(x => x.Id == AssemblyId);
            await WaitUntilAsync(() => viewModel.SelectedVersion?.Version == 2 && !viewModel.IsBusy
                && viewModel.Composition.TreeRoots.Count == 1 && viewModel.Composition.TotalMassKg == 12.5m,
                "Current assembly card, tree, and calculation did not load in time.");
            Assert.False(viewModel.CanEditCompositionUi);
            Assert.Equal(CurrentChildId, Assert.Single(viewModel.Composition.TreeRoots).Children.Single().Node.ObjectId);

            var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
            tabs.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            await Task.Delay(50);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var compositionEditor = FindCompositionEditor(window);
            Assert.False(compositionEditor.IsEffectivelyEnabled);

            viewModel.SelectedHistoryVersion = viewModel.SelectedCard!.Versions.Single(x => x.Version == 1);
            await WaitUntilAsync(() => viewModel.SelectedVersion?.Version == 1 && !viewModel.IsBusy
                && viewModel.Composition.Components.Count == 1 && viewModel.CanEditCompositionUi,
                "Selected historical composition did not load in time.");
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.True(compositionEditor.IsEffectivelyEnabled);
            var quantityEditor = compositionEditor.GetVisualDescendants().OfType<TextBox>()
                .Single(x => x.Watermark?.ToString() == "Количество");
            Assert.True(quantityEditor.IsEffectivelyEnabled);

            var component = Assert.Single(viewModel.Composition.Components);
            Assert.Equal(HistoricalChildId, component.ChildObjectId);
            Assert.Equal("АБВГ.123456.002 — Historical part", component.Label);
            var treeView = window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
            var treeRoot = treeView?.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault();
            if (treeRoot is not null)
            {
                treeRoot.IsExpanded = true;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
            }
            Assert.Equal("Действующий состав", window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "Действующий состав").Text);
            Assert.Equal("Расчёт действующего состава", window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Text == "Расчёт действующего состава").Text);
            var tabHeaders = window.GetVisualDescendants().OfType<TabItem>().Select(x => x.Header?.ToString()).ToArray();
            Assert.Contains("Карточка объекта", tabHeaders);
            Assert.Contains("Состав и расчёт", tabHeaders);
            Assert.Contains("Отчёт импорта", tabHeaders);
            Assert.Contains("Фоновые задачи", tabHeaders);
            Assert.True(window.Bounds.Width >= 1120);
            Assert.True(window.Bounds.Height >= 700);
            Assert.True(bindingErrors.Messages.Count == 0, string.Join(Environment.NewLine, bindingErrors.Messages));

            try
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                using var frame = window.CaptureRenderedFrame();
                frame?.Save("/tmp/pdm-desktop-preview.png");
            }
            catch (NotSupportedException)
            {
                // A rendered frame is optional on headless backends without bitmap capture.
            }
        }
        finally
        {
            window.Close();
            viewModel.Dispose();
            Logger.Sink = previousLogSink;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string failureMessage)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10);
        Assert.True(condition(), failureMessage);
    }

    private static bool GetIsPicking(MainWindow window) => (bool)typeof(MainWindow)
        .GetField("_isPicking", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(window)!;

    private static Border FindCompositionEditor(MainWindow window) =>
        Assert.IsType<Border>(window.FindControl<Border>("CompositionEditor"));

    private sealed class ControlledFolderPicker : IImportFolderPicker
    {
        private readonly TaskCompletionSource<SelectedImportPackage?> _result =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SelectedImportPackage?> PickAsync(Window owner, CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            return _result.Task;
        }

        public void Complete(SelectedImportPackage? package) => _result.TrySetResult(package);
    }

    private sealed class RepeatingFolderPicker(string path) : IImportFolderPicker
    {
        public int CallCount { get; private set; }

        public Task<SelectedImportPackage?> PickAsync(Window owner, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<SelectedImportPackage?>(new SelectedImportPackage([path]));
        }
    }

    private sealed class DesktopApiHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        public List<Guid> ImportIds { get; } = [];
        public int ImportFailuresRemaining { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.StartsWith("/api/imports/", StringComparison.Ordinal))
            {
                var importId = Guid.Parse(path["/api/imports/".Length..]);
                if (request.Method == HttpMethod.Get)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{\"code\":\"NotFound\",\"message\":\"Report not found.\"}", Encoding.UTF8, "application/json")
                    });
                if (request.Method == HttpMethod.Post)
                {
                    ImportIds.Add(importId);
                    if (ImportFailuresRemaining > 0)
                    {
                        ImportFailuresRemaining--;
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        {
                            Content = new StringContent("{\"code\":\"OutcomeUnknown\",\"message\":\"Temporary response failure.\"}", Encoding.UTF8, "application/json")
                        });
                    }
                    return Ok(new ImportReportDto(importId,
                    [new ImportFileResultDto("test.a3d", ImportFileStatus.Accepted, null,
                        ImportFileAction.Created, [])]));
                }
            }

            if (request.Method == HttpMethod.Get && path == "/api/objects")
                return Ok(new ObjectSearchPageDto(
                [
                    new(AssemblyId, "Assembly", "АБВГ.123456.001", "Main assembly", CurrentVersionId, 2, "Approved", null, ConcurrencyToken, false),
                    new(HistoricalChildId, "Part", "АБВГ.123456.002", "Historical part", null, null, null, null, Guid.NewGuid(), true),
                    new(CurrentChildId, "StandardPart", null, "Current fastener", null, null, null, null, Guid.NewGuid(), true)
                ], 0, 50, false));

            if (request.Method == HttpMethod.Get && path == "/api/background-tasks")
                return Ok(Array.Empty<BackgroundTaskDto>());

            if (request.Method == HttpMethod.Get && path == $"/api/objects/{AssemblyId:D}")
            {
                var historical = request.RequestUri.Query.Contains("version=1", StringComparison.Ordinal);
                return Ok(CreateCard(historical));
            }

            if (request.Method == HttpMethod.Get && path == $"/api/objects/{AssemblyId:D}/versions/1/composition")
                return Ok(new VersionCompositionDto(AssemblyId, 1, ConcurrencyToken,
                    [new(HistoricalChildId, 4, "Part", "АБВГ.123456.002", "Historical part", false)]));

            if (request.Method == HttpMethod.Get && path == $"/api/objects/{AssemblyId:D}/versions/2/composition")
                return Ok(new VersionCompositionDto(AssemblyId, 2, ConcurrencyToken, []));

            if (request.Method == HttpMethod.Get && path == $"/api/objects/{AssemblyId:D}/composition")
                return Ok(new CompositionTreeDto(AssemblyId,
                [
                    new(AssemblyId, [AssemblyId], null, 1, "Assembly", "АБВГ.123456.001", "Main assembly", null,
                        CurrentVersionId, 2, "Approved", null, null, null),
                    new(CurrentChildId, [AssemblyId, CurrentChildId], [AssemblyId], 2, "StandardPart", null,
                        "Current fastener", null, Guid.NewGuid(), 1, "Approved", 0.25m, null, null)
                ]));

            if (request.Method == HttpMethod.Get && path == $"/api/objects/{AssemblyId:D}/calculations")
                return Ok(new CompositionCalculationDto(AssemblyId, 12.5m, true,
                    [new(CurrentChildId, "StandardPart", null, "Current fastener", null, Guid.NewGuid(), 1, 2m, 0.25m, 0.5m)], []));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"code\":\"NotFound\",\"message\":\"Test route not found.\"}", Encoding.UTF8, "application/json")
            });
        }

        private static ObjectCardDto CreateCard(bool historical)
        {
            var selectedId = historical ? HistoricalVersionId : CurrentVersionId;
            var versionNumber = historical ? 1 : 2;
            var selectedState = historical ? "InWork" : "Approved";
            return new ObjectCardDto(AssemblyId, "Assembly", "АБВГ.123456.001", "Main assembly", CurrentVersionId,
                ConcurrencyToken,
                new ObjectVersionDto(selectedId, versionNumber, selectedState,
                    historical ? "Historical name" : "Current name", null, null, null, !historical),
                [
                    new ObjectVersionSummaryDto(CurrentVersionId, 2, "Approved", true),
                    new ObjectVersionSummaryDto(HistoricalVersionId, 1, "InWork", false)
                ], null, null);
        }

        private static Task<HttpResponseMessage> Ok<T>(T response) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response, JsonOptions), Encoding.UTF8, "application/json")
        });
    }

    private sealed class BindingErrorCollector : ILogSink
    {
        public List<string> Messages { get; } = [];
        public bool IsEnabled(LogEventLevel level, string area) => true;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            if (level >= LogEventLevel.Warning && area == LogArea.Binding)
                Messages.Add(messageTemplate);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
            params object?[] propertyValues)
        {
            if (level >= LogEventLevel.Warning && area == LogArea.Binding)
                Messages.Add($"{messageTemplate} :: {string.Join(", ", propertyValues.Select(x => x?.ToString() ?? "<null>"))}");
        }
    }
}
