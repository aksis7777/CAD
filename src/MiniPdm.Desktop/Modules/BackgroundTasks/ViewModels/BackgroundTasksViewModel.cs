using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.ViewModels;

namespace MiniPdm.Desktop.Modules.BackgroundTasks.ViewModels;

public sealed class BackgroundTasksViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPdmApiClient _client;
    private readonly AsyncCommand _refreshCommand;
    private readonly AsyncCommand _saveScheduleCommand;
    private readonly AsyncCommand _runCommand;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _polling;
    private BackgroundTaskDto? _selectedTask;
    private string _intervalMinutesText = string.Empty;
    private string _statusText = string.Empty;
    private bool _isBusy;
    private bool _disposed;

    public BackgroundTasksViewModel(IPdmApiClient client, TimeSpan? pollInterval = null, int maxPollCount = 20)
    {
        _client = client;
        PollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        MaxPollCount = Math.Max(0, maxPollCount);
        _refreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy, HandleError);
        _saveScheduleCommand = new AsyncCommand(SaveScheduleAsync, CanSaveSchedule, HandleError);
        _runCommand = new AsyncCommand(RunAsync, () => !IsBusy && SelectedTask is not null && !IsActive(SelectedTask.State), HandleError);
        RefreshCommand = _refreshCommand;
        SaveScheduleCommand = _saveScheduleCommand;
        RunCommand = _runCommand;
        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<BackgroundTaskDto> Tasks { get; } = [];
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveScheduleCommand { get; }
    public AsyncCommand RunCommand { get; }
    public TimeSpan PollInterval { get; }
    public int MaxPollCount { get; }

    public BackgroundTaskDto? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (!Set(ref _selectedTask, value)) return;
            IntervalMinutesText = value?.IntervalMinutes.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            RefreshCommands();
        }
    }

    public string IntervalMinutesText
    {
        get => _intervalMinutesText;
        set { if (Set(ref _intervalMinutesText, value)) RefreshCommands(); }
    }

    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) RefreshCommands(); } }
    public bool IsScheduleEditable => !IsBusy && SelectedTask is not null;

    public async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            var selectedId = SelectedTask?.Id;
            var rows = await _client.GetBackgroundTasksAsync(_lifetime.Token);
            Tasks.Clear();
            foreach (var row in rows) Tasks.Add(row);
            SelectedTask = selectedId is null ? Tasks.FirstOrDefault() : Tasks.FirstOrDefault(x => x.Id == selectedId) ?? Tasks.FirstOrDefault();
            StatusText = $"Загружено задач: {Tasks.Count}.";
        });
    }

    public void CancelActivePolling() => _polling?.Cancel();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _polling?.Cancel();
        _polling?.Dispose();
        _polling = null;
    }

    private bool CanSaveSchedule() => !IsBusy && SelectedTask is not null &&
        int.TryParse(IntervalMinutesText, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) && minutes is >= 1 and <= 525600;

    private async Task SaveScheduleAsync()
    {
        if (SelectedTask is null || !int.TryParse(IntervalMinutesText, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)) return;
        var id = SelectedTask.Id;
        await RunBusyAsync(async () =>
        {
            var updated = await _client.UpdateBackgroundTaskScheduleAsync(id, new UpdateBackgroundTaskScheduleRequestDto(minutes), _lifetime.Token);
            Upsert(updated);
            SelectedTask = updated;
            StatusText = "Расписание сохранено.";
        });
    }

    private async Task RunAsync()
    {
        if (SelectedTask is null) return;
        var id = SelectedTask.Id;
        var lifetimeToken = _lifetime.Token;
        await RunBusyAsync(async () =>
        {
            await _client.RunBackgroundTaskAsync(id, lifetimeToken);
            StatusText = "Задача запущена.";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
            _polling = cts;
            try
            {
                for (var attempt = 0; attempt < MaxPollCount; attempt++)
                {
                    await Task.Delay(PollInterval, cts.Token);
                    var rows = await _client.GetBackgroundTasksAsync(cts.Token);
                    foreach (var row in rows) Upsert(row);
                    var current = Tasks.FirstOrDefault(x => x.Id == id);
                    if (current is not null)
                    {
                        SelectedTask = current;
                        if (!IsActive(current.State))
                        {
                            StatusText = current.LastError is { Length: > 0 } error ? error : current.LastResult ?? "Задача завершена.";
                            return;
                        }
                    }
                }
                var final = Tasks.FirstOrDefault(x => x.Id == id);
                if (final is not null) SelectedTask = final;
                StatusText = "Задача ещё выполняется. Обновите состояние позже.";
            }
            finally { if (ReferenceEquals(_polling, cts)) _polling = null; }
        });
    }

    private static bool IsActive(string state) => state.Equals("Queued", StringComparison.OrdinalIgnoreCase)
        || state.Equals("Running", StringComparison.OrdinalIgnoreCase);

    private void Upsert(BackgroundTaskDto task)
    {
        var index = -1;
        for (var i = 0; i < Tasks.Count; i++) if (Tasks[i].Id == task.Id) { index = i; break; }
        if (index < 0) Tasks.Add(task); else Tasks[index] = task;
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (PdmApiException ex) { StatusText = ex.Message; }
        catch (Exception ex) { StatusText = ex.Message; }
        finally { IsBusy = false; }
    }

    private void HandleError(Exception ex) => StatusText = ex.Message;

    private void RefreshCommands()
    {
        _refreshCommand.Refresh();
        _saveScheduleCommand.Refresh();
        _runCommand.Refresh();
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name is nameof(IsBusy) or nameof(SelectedTask)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScheduleEditable)));
        return true;
    }
}
