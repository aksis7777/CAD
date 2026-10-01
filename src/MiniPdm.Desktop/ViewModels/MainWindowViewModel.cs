using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.Modules.Import.ViewModels;
using MiniPdm.Desktop.Modules.BackgroundTasks.ViewModels;
using MiniPdm.Desktop.Modules.Composition.ViewModels;

namespace MiniPdm.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPdmApiClient _client;
    private readonly AsyncCommand _refreshCommand;
    private readonly AsyncCommand _loadMoreCommand;
    private readonly AsyncCommand _saveAttributesCommand;
    private readonly AsyncCommand _saveCompositionCommand;
    private readonly AsyncCommand _cloneCommand;
    private readonly AsyncCommand _approveCommand;
    private readonly AsyncCommand _cancelVersionCommand;
    private readonly AsyncCommand _searchChildrenCommand;
    private readonly AsyncCommand _refreshSelectedCommand;
    private ObjectSearchItemDto? _selectedObject;
    private ObjectCardDto? _selectedCard;
    private ObjectVersionSummaryDto? _selectedHistoryVersion;
    private string _searchText = string.Empty;
    private string _childSearchText = string.Empty;
    private string _nameText = string.Empty;
    private string _materialText = string.Empty;
    private string _massText = string.Empty;
    private string _statusText = "Подключение к серверу…";
    private string _messageText = string.Empty;
    private bool _isBusy;
    private int _offset;
    private bool _hasMore;
    private bool _isConnected;
    private CancellationTokenSource? _selectionCts;
    private bool _suppressHistoryLoad;
    private bool _requiresReview;
    private Guid? _compositionConcurrencyToken;

    public MainWindowViewModel(IPdmApiClient client)
    {
        _client = client;
        BackgroundTasks = new BackgroundTasksViewModel(client);
        Import = new ImportViewModel(client, RefreshAfterImportAsync);
        Composition = new CompositionWorkspaceViewModel(SelectTreeObject);
        _refreshCommand = new AsyncCommand(() => RefreshAsync(resetOffset: true), () => !IsBusy, HandleCommandError);
        _loadMoreCommand = new AsyncCommand(LoadMoreAsync, () => !IsBusy && HasMore, HandleCommandError);
        _saveAttributesCommand = new AsyncCommand(SaveAttributesAsync, CanEditSelectedVersion, HandleCommandError);
        _saveCompositionCommand = new AsyncCommand(SaveCompositionAsync, CanEditComposition, HandleCommandError);
        _cloneCommand = new AsyncCommand(CloneAsync, () => !IsBusy && SelectedCard?.Versions.Count > 0, HandleCommandError);
        _approveCommand = new AsyncCommand(() => ChangeStateAsync("Approved"), () => !IsBusy && SelectedVersion?.State == "InWork", HandleCommandError);
        _cancelVersionCommand = new AsyncCommand(() => ChangeStateAsync("Cancelled"), () => !IsBusy && SelectedVersion?.State is "InWork" or "Approved", HandleCommandError);
        _searchChildrenCommand = new AsyncCommand(SearchChildrenAsync, () => !IsBusy, HandleCommandError);
        _refreshSelectedCommand = new AsyncCommand(LoadSelectedObjectAsync, () => !IsBusy && SelectedObject is not null, HandleCommandError);
        RefreshCommand = _refreshCommand;
        LoadMoreCommand = _loadMoreCommand;
        SaveAttributesCommand = _saveAttributesCommand;
        SaveCompositionCommand = _saveCompositionCommand;
        CloneCommand = _cloneCommand;
        ApproveCommand = _approveCommand;
        CancelVersionCommand = _cancelVersionCommand;
        SearchChildrenCommand = _searchChildrenCommand;
        RefreshSelectedCommand = _refreshSelectedCommand;
        _ = RefreshAsync(resetOffset: true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<ObjectSearchItemDto> Objects { get; } = [];
    public BackgroundTasksViewModel BackgroundTasks { get; }
    public ImportViewModel Import { get; }
    public CompositionWorkspaceViewModel Composition { get; }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand LoadMoreCommand { get; }
    public AsyncCommand SaveAttributesCommand { get; }
    public AsyncCommand SaveCompositionCommand { get; }
    public AsyncCommand CloneCommand { get; }
    public AsyncCommand ApproveCommand { get; }
    public AsyncCommand CancelVersionCommand { get; }
    public AsyncCommand SearchChildrenCommand { get; }
    public AsyncCommand RefreshSelectedCommand { get; }

    public string SearchText { get => _searchText; set => Set(ref _searchText, value); }
    public string ChildSearchText { get => _childSearchText; set => Set(ref _childSearchText, value); }
    public string NameText { get => _nameText; set => Set(ref _nameText, value); }
    public string MaterialText { get => _materialText; set => Set(ref _materialText, value); }
    public string MassText { get => _massText; set => Set(ref _massText, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }
    public string MessageText { get => _messageText; private set => Set(ref _messageText, value); }
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) RefreshCommands(); } }
    public bool IsConnected { get => _isConnected; private set => Set(ref _isConnected, value); }
    public bool HasMore { get => _hasMore; private set { if (Set(ref _hasMore, value)) _loadMoreCommand.Refresh(); } }
    public string? SelectedObjectLabel => SelectedObject is null ? null : ObjectLabel(SelectedObject);
    public string? SelectedObjectName => SelectedObject?.Name;
    public string? SelectedCardType => SelectedCard?.Type;
    public IReadOnlyList<ObjectVersionSummaryDto> HistoryVersions => SelectedCard?.Versions ?? Array.Empty<ObjectVersionSummaryDto>();
    public string? SelectedVersionLabel => SelectedVersion is null ? null : $"Версия {SelectedVersion.Version} — {StateLabel(SelectedVersion.State)}";
    public string CurrentVersionLabel => SelectedCard?.Versions.FirstOrDefault(x => x.IsCurrent) is { } current
        ? $"Текущая версия: {current.Version} — {StateLabel(current.State)}" : "Текущей действующей версии нет";
    public ObjectVersionDto? SelectedVersion => SelectedCard?.SelectedVersion;
    public bool IsStandardPart => SelectedObject?.Type == "StandardPart";
    public bool IsAssembly => SelectedObject?.Type == "Assembly";
    public bool IsEditable => SelectedVersion?.State == "InWork";
    public bool CanEditCompositionUi => CanEditComposition();
    public bool CanEditAttributes => IsEditable && !IsBusy && !IsReviewRequired;
    public bool CanEditName => SelectedCard?.SelectedVersion is not null && !IsStandardPart;
    public bool CanEditMaterial => SelectedCard?.SelectedVersion is not null && SelectedObject?.Type == "Part";
    public bool CanEditMass => SelectedCard?.SelectedVersion is not null && (SelectedObject?.Type is "Part" or "StandardPart");
    public bool CanApprove => SelectedVersion?.State == "InWork";
    public bool CanCancel => SelectedVersion?.State is "InWork" or "Approved";
    public string? SelectedSourceReference => SelectedVersion?.SourceReference;
    public bool IsReviewRequired { get => _requiresReview; private set { if (Set(ref _requiresReview, value)) RefreshCommands(); } }

    public ObjectSearchItemDto? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (!Set(ref _selectedObject, value)) return;
            Notify(nameof(SelectedObjectLabel));
            Notify(nameof(SelectedObjectName));
            IsReviewRequired = false;
            Composition.CanEdit = false;
            _ = LoadSelectedObjectAsync();
        }
    }
    public ObjectCardDto? SelectedCard { get => _selectedCard; private set { if (Set(ref _selectedCard, value)) NotifyCardProperties(); } }
    public ObjectVersionSummaryDto? SelectedHistoryVersion
    {
        get => _selectedHistoryVersion;
        set
        {
            if (_suppressHistoryLoad) { Set(ref _selectedHistoryVersion, value); return; }
            if (IsBusy || !Set(ref _selectedHistoryVersion, value)) return;
            Notify(nameof(SelectedVersionLabel));
            if (SelectedObject is not null && value is not null) _ = LoadVersionAsync(value.Version);
        }
    }
    public Task ImportFolderAsync(IReadOnlyList<string> filePaths) => Import.ImportFilesAsync(filePaths);
    public void ReportError(string message) => MessageText = message;

    private async Task RefreshAfterImportAsync()
    {
        if (IsBusy)
        {
            MessageText = "Импорт подтверждён. Обновите список после завершения текущей операции.";
            return;
        }
        await RefreshAsync(resetOffset: true);
    }

    private async Task RefreshAsync(bool resetOffset, bool insideBusy = false)
    {
        if (!insideBusy) await RunBusyAsync(() => RefreshAsync(resetOffset, insideBusy: true));
        else
        {
            if (resetOffset) { _offset = 0; Objects.Clear(); }
            try
            {
                var page = await _client.SearchObjectsAsync(SearchText, _offset, 50);
                foreach (var item in page.Items) Objects.Add(item);
                _offset = page.Offset + page.Items.Count;
                HasMore = page.HasMore;
                IsConnected = true;
                StatusText = $"Сервер доступен · объектов на экране: {Objects.Count}";
                if (SelectedObject is not null)
                {
                    var updated = Objects.FirstOrDefault(x => x.Id == SelectedObject.Id);
                    if (updated is not null && Set(ref _selectedObject, updated))
                    {
                        Notify(nameof(SelectedObjectLabel));
                        Notify(nameof(SelectedObjectName));
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusText = $"Нет связи с сервером: {ex.Message}";
            }
        }
    }

    private async Task LoadMoreAsync() => await RunBusyAsync(async () =>
    {
        var page = await _client.SearchObjectsAsync(SearchText, _offset, 50);
        foreach (var item in page.Items) Objects.Add(item);
        _offset = page.Offset + page.Items.Count;
        HasMore = page.HasMore;
    });

    private async Task LoadSelectedObjectAsync()
    {
        if (SelectedObject is null) { SelectedCard = null; return; }
        var objectId = SelectedObject.Id;
        IsReviewRequired = false;
        _selectionCts?.Cancel();
        _selectionCts?.Dispose();
        var cts = _selectionCts = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            var card = await _client.GetObjectAsync(objectId, cancellationToken: cts.Token);
            if (!IsCurrentSelection(objectId, cts)) return;
            if (card.SelectedVersion is null && card.Versions.Count > 0)
            {
                var fallback = card.Versions.OrderByDescending(x => x.Version).First();
                card = await _client.GetObjectAsync(objectId, fallback.Version, cts.Token);
            }
            var loaded = await LoadConsistentCardCompositionAsync(objectId, card, cts.Token);
            card = loaded.Card;
            if (!IsCurrentSelection(objectId, cts)) return;
            ApplyCard(card, updateEditor: true);
            ApplyEditableComposition(loaded.Composition);
            await LoadPanelsAsync(objectId, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (Exception ex) { if (IsCurrentSelection(objectId, cts)) MessageText = $"Не удалось открыть объект: {ex.Message}"; }
        finally
        {
            if (ReferenceEquals(_selectionCts, cts)) IsBusy = false;
        }
    }

    private async Task LoadVersionAsync(int version)
    {
        if (SelectedObject is null) return;
        var objectId = SelectedObject.Id;
        IsReviewRequired = false;
        _selectionCts?.Cancel();
        _selectionCts?.Dispose();
        var cts = _selectionCts = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            var card = await _client.GetObjectAsync(objectId, version, cts.Token);
            if (!IsCurrentSelection(objectId, cts)) return;
            var loaded = await LoadConsistentCardCompositionAsync(objectId, card, cts.Token);
            card = loaded.Card;
            if (!IsCurrentSelection(objectId, cts)) return;
            ApplyCard(card, updateEditor: true);
            ApplyEditableComposition(loaded.Composition);
            await LoadPanelsAsync(objectId, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (Exception ex) { if (IsCurrentSelection(objectId, cts)) MessageText = $"Не удалось открыть версию: {ex.Message}"; }
        finally
        {
            if (ReferenceEquals(_selectionCts, cts)) IsBusy = false;
        }
    }

    private async Task<(ObjectCardDto Card, VersionCompositionDto? Composition)> LoadConsistentCardCompositionAsync(
        Guid objectId, ObjectCardDto initialCard, CancellationToken cancellationToken)
    {
        var card = initialCard;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            VersionCompositionDto? composition = null;
            if (card.Type == "Assembly" && card.SelectedVersion is not null)
            {
                composition = await _client.GetVersionCompositionAsync(objectId, card.SelectedVersion.Version, cancellationToken);
                if (composition.ConcurrencyToken != card.ConcurrencyToken)
                {
                    card = await _client.GetObjectAsync(objectId, card.SelectedVersion.Version, cancellationToken);
                    continue;
                }
            }
            return (card, composition);
        }
        throw new InvalidOperationException("Карточка и состав изменились во время чтения. Обновите объект и повторите просмотр.");
    }

    private bool IsCurrentSelection(Guid objectId, CancellationTokenSource cts) =>
        !cts.IsCancellationRequested && ReferenceEquals(_selectionCts, cts) && SelectedObject?.Id == objectId;

    private void ApplyEditableComposition(VersionCompositionDto? composition)
    {
        _compositionConcurrencyToken = composition?.ConcurrencyToken;
        Composition.SetItems(composition?.Items ?? [], id =>
        {
            var obj = Objects.FirstOrDefault(x => x.Id == id);
            var source = composition?.Items.FirstOrDefault(x => x.ChildObjectId == id);
            return obj is null ? source?.Designation ?? source?.Name ?? id.ToString() : ObjectLabel(obj);
        });
        Composition.CanEdit = CanEditComposition();
        Notify(nameof(CanEditCompositionUi));
    }

    private async Task LoadPanelsAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tree = await _client.GetCompositionAsync(objectId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (SelectedObject?.Id != objectId) return;
            Composition.SetTree(tree);
            var calculation = await _client.GetCalculationAsync(objectId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (SelectedObject?.Id != objectId) return;
            Composition.SetCalculation(calculation);
        }
        catch (PdmApiException ex) when (ex.StatusCode == 404)
        {
            Composition.ClearCurrent();
        }
    }

    private void ApplyCard(ObjectCardDto card, bool updateEditor)
    {
        SelectedCard = card;
        _suppressHistoryLoad = true;
        SelectedHistoryVersion = card.Versions.FirstOrDefault(x => x.Version == card.SelectedVersion?.Version)
            ?? (card.SelectedVersion is null ? card.Versions.OrderByDescending(x => x.Version).FirstOrDefault() : null);
        _suppressHistoryLoad = false;
        Notify(nameof(SelectedSourceReference));
        if (updateEditor)
        {
            NameText = card.SelectedVersion?.Name ?? string.Empty;
            MaterialText = card.SelectedVersion?.Material ?? string.Empty;
            MassText = card.SelectedVersion?.UnitMassKg?.ToString("0.######", CultureInfo.GetCultureInfo("ru-RU")) ?? string.Empty;
        }
    }

    private async Task SearchChildrenAsync() => await RunBusyAsync(async () =>
    {
        var page = await _client.SearchObjectsAsync(ChildSearchText, 0, 30);
        Composition.ChildResults.Clear();
        foreach (var item in page.Items) Composition.ChildResults.Add(item);
    });

    private async Task SaveAttributesAsync()
    {
        if (SelectedObject is null || SelectedVersion is null || SelectedCard is null) return;
        decimal? mass = null;
        if (MassText.Length != 0)
        {
            const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            var ru = CultureInfo.GetCultureInfo("ru-RU");
            if (!decimal.TryParse(MassText, styles, CultureInfo.InvariantCulture, out var parsed)
                && !decimal.TryParse(MassText, styles, ru, out parsed))
            { MessageText = "Введите массу как десятичное число без округления, например 1,25."; return; }
            mass = parsed;
        }
        if (IsAssembly) mass = null;
        var objectId = SelectedObject.Id;
        var version = SelectedVersion.Version;
        var name = IsStandardPart ? SelectedObject.Name : NameText;
        var material = IsAssembly || IsStandardPart ? null : MaterialText;
        await ExecuteMutationAsync(token => _client.UpdateVersionAttributesAsync(objectId,
            version, new UpdateVersionAttributesRequestDto(name, material, mass, token)));
    }

    private async Task SaveCompositionAsync()
    {
        if (SelectedObject is null || SelectedVersion is null || SelectedCard is null || _compositionConcurrencyToken != SelectedCard.ConcurrencyToken) return;
        var components = new List<CompositionItemDto>();
        foreach (var item in Composition.Components)
        {
            if (!item.TryGetPositiveQuantity(out var quantity))
            {
                MessageText = $"Укажите для компонента «{item.Label}» положительное целое количество в диапазоне Int32.";
                return;
            }
            components.Add(new CompositionItemDto(item.ChildObjectId, quantity));
        }
        var objectId = SelectedObject.Id;
        var version = SelectedVersion.Version;
        var concurrencyToken = _compositionConcurrencyToken.Value;
        await ExecuteMutationAsync(_ => _client.ReplaceCompositionAsync(objectId,
            version, new ReplaceCompositionRequestDto(components, concurrencyToken)));
    }

    private async Task CloneAsync()
    {
        if (SelectedObject is null || SelectedVersion is null || SelectedCard is null) return;
        var objectId = SelectedObject.Id;
        var version = SelectedVersion.Version;
        await ExecuteMutationAsync(token => _client.CloneVersionAsync(objectId,
            new CloneVersionRequestDto(version, token)));
    }

    private async Task ChangeStateAsync(string state)
    {
        if (SelectedObject is null || SelectedVersion is null || SelectedCard is null) return;
        var objectId = SelectedObject.Id;
        var version = SelectedVersion.Version;
        await ExecuteMutationAsync(token => _client.ChangeVersionStateAsync(objectId,
            version, new ChangeVersionStateRequestDto(state, token)));
    }

    private async Task ExecuteMutationAsync(Func<Guid, Task<VersionMutationDto>> operation)
    {
        if (SelectedObject is null || SelectedCard is null) return;
        var objectId = SelectedObject.Id;
        var token = SelectedCard.ConcurrencyToken;
        if (IsReviewRequired) { MessageText = "Обновите карточку и проверьте изменения перед сохранением."; return; }
        await RunBusyAsync(async () =>
        {
            try
            {
                var result = await operation(token);
                IsReviewRequired = false;
                MessageText = result.Warnings.Count == 0 ? "Изменение сохранено." : string.Join(" ", result.Warnings);
                await RefreshCardAfterMutationAsync(objectId, result.VersionNumber);
            }
            catch (PdmApiException ex) when (ex.StatusCode == 409)
            {
                MessageText = $"Конфликт версии: {ex.Message} Редактор сохранён; данные карточки обновлены. Проверьте изменения и сохраните повторно.";
                IsReviewRequired = true;
                await RefreshCardAfterMutationAsync(objectId, SelectedVersion?.Version, updateEditor: false, refreshComposition: false);
            }
            catch (PdmApiException ex) when (ex.IsOutcomeUnknown)
            {
                MessageText = $"Сервер не подтвердил результат. Карточка обновлена; проверьте её перед повторной командой. {ex.Message}";
                IsReviewRequired = true;
                await RefreshCardAfterMutationAsync(objectId, SelectedVersion?.Version, updateEditor: false, refreshComposition: false);
            }
            catch (Exception ex) { MessageText = ex.Message; }
        });
    }

    private async Task RefreshCardAfterMutationAsync(Guid objectId, int? version, bool updateEditor = true, bool refreshComposition = true)
    {
        var card = await _client.GetObjectAsync(objectId, version);
        ApplyCard(card, updateEditor);
        if (SelectedObject is not null)
        {
            var refreshed = await _client.SearchObjectsAsync(SearchText, 0, 100);
            var row = refreshed.Items.FirstOrDefault(x => x.Id == objectId);
            if (row is not null)
            {
                var oldRow = Objects.FirstOrDefault(x => x.Id == objectId);
                if (oldRow is not null) Objects[Objects.IndexOf(oldRow)] = row;
            }
        }
        if (refreshComposition)
        {
            var loaded = await LoadConsistentCardCompositionAsync(objectId, card, default);
            ApplyCard(loaded.Card, updateEditor);
            if (updateEditor) ApplyEditableComposition(loaded.Composition);
            await LoadPanelsAsync(objectId);
        }
    }

    private bool CanEditSelectedVersion() => !IsBusy && !IsReviewRequired && IsEditable && SelectedCard is not null;
    private bool CanEditComposition() => !IsBusy && !IsReviewRequired && IsAssembly && IsEditable && SelectedCard is not null
        && _compositionConcurrencyToken == SelectedCard.ConcurrencyToken;

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }

    private void RefreshCommands()
    {
        _refreshCommand.Refresh(); _loadMoreCommand.Refresh(); _saveAttributesCommand.Refresh();
        _saveCompositionCommand.Refresh(); _cloneCommand.Refresh(); _approveCommand.Refresh();
        _cancelVersionCommand.Refresh(); _searchChildrenCommand.Refresh();
        _refreshSelectedCommand.Refresh();
        Composition.CanEdit = CanEditComposition();
        Notify(nameof(CanEditCompositionUi));
        Notify(nameof(IsEditable)); Notify(nameof(CanApprove)); Notify(nameof(CanCancel));
        Notify(nameof(CanEditAttributes)); Notify(nameof(CanEditName)); Notify(nameof(CanEditMaterial)); Notify(nameof(CanEditMass));
    }

    private void NotifyCardProperties()
    {
        Notify(nameof(SelectedCardType)); Notify(nameof(HistoryVersions));
        Notify(nameof(SelectedVersion)); Notify(nameof(SelectedVersionLabel)); Notify(nameof(IsStandardPart));
        Notify(nameof(IsAssembly)); Notify(nameof(IsEditable));
        Notify(nameof(CanEditAttributes)); Notify(nameof(CanEditName)); Notify(nameof(CanEditMaterial)); Notify(nameof(CanEditMass));
        Notify(nameof(CurrentVersionLabel));
        _saveAttributesCommand.Refresh(); _saveCompositionCommand.Refresh(); _cloneCommand.Refresh();
        _approveCommand.Refresh(); _cancelVersionCommand.Refresh();
        Composition.CanEdit = CanEditComposition();
        Notify(nameof(CanEditCompositionUi));
    }

    private void HandleCommandError(Exception exception) => MessageText = exception.Message;
    private void SelectTreeObject(Guid objectId)
    {
        if (IsBusy) return;
        var node = Composition.TreeRoots.SelectMany(FlattenTree).FirstOrDefault(x => x.Node.ObjectId == objectId);
        if (node is null) return;
        var item = Objects.FirstOrDefault(x => x.Id == objectId);
        if (item is null)
        {
            item = new ObjectSearchItemDto(node.Node.ObjectId, node.Node.Type, node.Node.Designation, node.Node.Name,
                node.Node.VersionId, node.Node.VersionNumber, node.Node.State, node.Node.UnitMassKg, Guid.Empty,
                node.Node.VersionId is null);
            Objects.Insert(0, item);
        }
        SelectedObject = item;
    }
    private static IEnumerable<TreeOccurrenceViewModel> FlattenTree(TreeOccurrenceViewModel root)
    {
        yield return root;
        foreach (var child in root.Children.SelectMany(FlattenTree)) yield return child;
    }
    private static string ObjectLabel(ObjectSearchItemDto item) => item.Type switch
    {
        "StandardPart" => item.Name ?? "Стандартное изделие",
        _ => $"{item.Designation ?? "Без обозначения"} — {item.Name ?? item.Type}"
    };
    private static string StateLabel(string state) => state switch { "InWork" => "В работе", "Approved" => "Утверждено", "Cancelled" => "Аннулировано", _ => state };

    public void Dispose()
    {
        _selectionCts?.Cancel();
        _selectionCts?.Dispose();
        _selectionCts = null;
        BackgroundTasks.Dispose();
        Import.Dispose();
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Notify(name); return true;
    }
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
