using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.ViewModels;

namespace MiniPdm.Desktop.Modules.Import.ViewModels;

public sealed class ImportViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPdmApiClient _client;
    private readonly Func<Task> _afterImport;
    private readonly AsyncCommand _retryCommand;
    private readonly AsyncCommand _abandonCommand;
    private readonly AsyncCommand _cancelCommand;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _isBusy;
    private bool _disposed;
    private CancellationTokenSource? _activeOperation;
    private string _statusText = string.Empty;
    private Guid? _pendingImportId;
    private string[] _pendingFiles = [];

    public ImportViewModel(IPdmApiClient client, Func<Task> afterImport)
    {
        _client = client;
        _afterImport = afterImport;
        _retryCommand = new AsyncCommand(RetryAsync, () => !IsBusy && _pendingImportId.HasValue, HandleError);
        _abandonCommand = new AsyncCommand(AbandonPendingAsync, () => !IsBusy && _pendingImportId.HasValue, HandleError);
        _cancelCommand = new AsyncCommand(CancelActiveOperationAsync, () => IsBusy, HandleError);
        RetryCommand = _retryCommand;
        AbandonPendingCommand = _abandonCommand;
        CancelCommand = _cancelCommand;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<ImportFileResultDto> Files { get; } = [];
    public AsyncCommand RetryCommand { get; }
    public AsyncCommand AbandonPendingCommand { get; }
    public AsyncCommand CancelCommand { get; }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) RefreshCommands(); } }
    public bool CanStartNewImport => !IsBusy && !_pendingImportId.HasValue;
    public bool HasPendingImport => _pendingImportId.HasValue;
    public Guid? PendingImportId => _pendingImportId;

    public async Task ImportFilesAsync(IReadOnlyList<string> paths)
    {
        if (IsBusy) { StatusText = "Предыдущая операция ещё выполняется."; return; }
        if (_pendingImportId.HasValue)
        {
            StatusText = $"Пакет {_pendingImportId} не подтверждён. Повторите проверку или явно откажитесь от его повтора.";
            return;
        }
        var files = paths.Where(x => Path.GetExtension(x).Equals(".a3d", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(x).Equals(".m3d", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (files.Length == 0) { StatusText = "В папке не найдены файлы .a3d или .m3d."; return; }
        _pendingFiles = files;
        _pendingImportId = Guid.NewGuid();
        RefreshCommands();
        await RetryAsync();
    }

    private async Task RetryAsync()
    {
        if (_pendingImportId is not Guid importId || IsBusy || _disposed) return;
        IsBusy = true;
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _activeOperation = operation;
        var ct = operation.Token;
        try
        {
            try
            {
                var report = await _client.GetImportReportAsync(importId, ct);
                if (report is not null)
                {
                    ShowReport(report);
                    Complete();
                    await RefreshCatalogAsync(importId);
                    return;
                }
            }
            catch (PdmApiException ex) when (ex.StatusCode == 404) { }

            try
            {
                ShowReport(await _client.ImportFilesAsync(importId, _pendingFiles, ct));
                Complete();
                await RefreshCatalogAsync(importId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (!_lifetime.IsCancellationRequested)
                    StatusText = $"Запрос отменён клиентом. Исход пакета {_pendingImportId} неизвестен; проверьте отчёт и повторите с тем же ID.";
            }
            catch (Exception ex)
            {
                StatusText = ex is PdmApiException { IsOutcomeUnknown: true }
                    ? $"Результат неизвестен. Повторите проверку с тем же ID {importId}."
                    : $"Импорт не подтверждён (ID {importId}): {ex.Message}";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            if (!_lifetime.IsCancellationRequested)
                StatusText = $"Запрос отменён клиентом. Исход пакета {_pendingImportId} неизвестен; проверьте отчёт и повторите с тем же ID.";
        }
        catch (Exception ex) { StatusText = $"Не удалось проверить пакет {importId}: {ex.Message}"; }
        finally { if (ReferenceEquals(_activeOperation, operation)) _activeOperation = null; IsBusy = false; }
    }

    private async Task RefreshCatalogAsync(Guid importId)
    {
        try { await _afterImport(); }
        catch (Exception ex) { StatusText = $"Импорт {importId} подтверждён, но список объектов не обновился: {ex.Message}"; }
    }

    private Task AbandonPendingAsync()
    {
        var id = _pendingImportId;
        _pendingImportId = null;
        _pendingFiles = [];
        StatusText = id is null ? string.Empty : $"Повтор пакета {id} отменён оператором. Теперь можно выбрать новый пакет.";
        RefreshCommands();
        return Task.CompletedTask;
    }

    private Task CancelActiveOperationAsync()
    {
        _activeOperation?.Cancel();
        return Task.CompletedTask;
    }

    private void ShowReport(ImportReportDto report)
    {
        Files.Clear();
        foreach (var file in report.Files) Files.Add(file);
        StatusText = $"Принято: {report.AcceptedCount}; отклонено: {report.RejectedCount}; файлов с предупреждениями: {report.WarningCount}. ID: {report.ImportId}";
    }

    private void Complete()
    {
        _pendingImportId = null;
        _pendingFiles = [];
        RefreshCommands();
    }

    private void HandleError(Exception exception) => StatusText = exception.Message;
    private void RefreshCommands()
    {
        _retryCommand.Refresh();
        _abandonCommand.Refresh();
        _cancelCommand.Refresh();
        Notify(nameof(CanStartNewImport));
        Notify(nameof(HasPendingImport));
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Notify(name);
        return true;
    }
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _activeOperation?.Cancel();
        _lifetime.Dispose();
    }
}
