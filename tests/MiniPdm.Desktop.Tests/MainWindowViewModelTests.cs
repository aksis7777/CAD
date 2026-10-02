using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.Modules.Import.ViewModels;
using MiniPdm.Desktop.Services.ImportFolderPickers;
using MiniPdm.Desktop.ViewModels;
using Xunit;

namespace MiniPdm.Desktop.Tests;

/// <summary>
/// Проверяет выбор объектов, изменение состава и обработку импорта в модели представления главного окна.
/// </summary>
public sealed class MainWindowViewModelTests
{
    /// <summary>
    /// Проверяет, что запоздавший ответ карточки не заменяет сведения о последнем выбранном объекте.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task LateObjectCardResponseCannotReplaceTheMostRecentSelection()
    {
        var client = new FakeClient();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.GetObjectOverride = async (id, version, ct) =>
        {
            if (id == firstId)
            {
                firstEntered.TrySetResult();
                await releaseFirst.Task;
                return Card(firstId, "Old selection", token: Guid.NewGuid());
            }
            return Card(secondId, "New selection", token: Guid.NewGuid());
        };
        using var viewModel = new MainWindowViewModel(client);
        viewModel.SelectedObject = Item(firstId, "A");
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        viewModel.SelectedObject = Item(secondId, "B");
        await WaitUntilAsync(() => viewModel.SelectedCard?.Id == secondId && !viewModel.IsBusy);
        releaseFirst.TrySetResult();
        await Task.Delay(30);

        Assert.Equal(secondId, viewModel.SelectedCard!.Id);
        Assert.Equal("New selection", viewModel.NameText);
    }

    /// <summary>
    /// Проверяет выбор исторической аннулированной версии и загрузку её состава, когда у объекта нет текущей версии.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task NoCurrentObjectSelectsHistoricalCancelledVersionAndLoadsItsComposition()
    {
        var client = new FakeClient();
        var objectId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var token = Guid.NewGuid();
        var historical = new ObjectVersionSummaryDto(Guid.NewGuid(), 3, "Cancelled", false);
        client.Cards[objectId] = Card(objectId, null, token, historical, "Assembly");
        client.VersionCompositions[(objectId, 3)] = new VersionCompositionDto(objectId, 3, token,
            [new VersionCompositionItemDto(childId, 4, "Part", "АБВГ.301245.001", "Стойка", true)]);
        using var viewModel = new MainWindowViewModel(client);

        viewModel.SelectedObject = Item(objectId, "АБВГ.301245.002", "Assembly");
        await WaitUntilAsync(() => viewModel.SelectedVersion?.Version == 3 && !viewModel.IsBusy);

        Assert.Equal("Cancelled", viewModel.SelectedVersion!.State);
        Assert.Single(viewModel.Composition.Components);
        Assert.Equal(childId, viewModel.Composition.Components[0].ChildObjectId);
        Assert.Equal("4", viewModel.Composition.Components[0].QuantityText);
        Assert.True(viewModel.CloneCommand.CanExecute(null));
        Assert.False(viewModel.SaveCompositionCommand.CanExecute(null));
    }

    /// <summary>
    /// Проверяет сохранение введённых значений после конфликта до явного обновления оператором.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task ConflictPreservesEditorValuesUntilOperatorExplicitlyRefreshes()
    {
        var client = new FakeClient();
        var objectId = Guid.NewGuid();
        var firstToken = Guid.NewGuid();
        var latestToken = Guid.NewGuid();
        client.CardFactory = (id, requestedVersion, number) => Card(id,
            number == 1 ? "Server value" : "Server value after conflict",
            number == 1 ? firstToken : latestToken);
        client.ConflictOnUpdate = true;
        using var viewModel = new MainWindowViewModel(client);
        viewModel.SelectedObject = Item(objectId, "АБВГ.301245.010");
        await WaitUntilAsync(() => viewModel.SelectedVersion?.Version == 1 && !viewModel.IsBusy);
        viewModel.NameText = "Operator edit";

        await viewModel.SaveAttributesCommand.ExecuteAsync();

        Assert.Equal("Operator edit", viewModel.NameText);
        Assert.True(viewModel.IsReviewRequired);
        Assert.False(viewModel.SaveAttributesCommand.CanExecute(null));
        Assert.Equal(1, client.UpdateCount);

        await viewModel.RefreshSelectedCommand.ExecuteAsync();

        Assert.False(viewModel.IsReviewRequired);
        Assert.Equal("Server value after conflict", viewModel.NameText);
    }

    /// <summary>
    /// Проверяет повтор не подтверждённого импорта с прежним идентификатором и исходным содержимым файлов.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task UnconfirmedImportRetryReusesTheSameIdAndOriginalFilePayload()
    {
        var client = new FakeClient { ImportOutcomeUnknown = true };
        using var viewModel = new ImportViewModel(client, () => Task.CompletedTask);
        var paths = new[] { "/cad/one.a3d", "/cad/notes.txt", "/cad/two.M3D" };

        await viewModel.ImportFilesAsync(paths);
        var pendingId = viewModel.PendingImportId;
        Assert.NotNull(pendingId);
        Assert.Single(client.UploadIds);
        Assert.Equal(new[] { paths[0], paths[2] }, client.UploadedFiles[0]);

        client.ReportAvailable = true;
        await viewModel.RetryCommand.ExecuteAsync();

        Assert.Null(viewModel.PendingImportId);
        Assert.Single(client.UploadIds);
        Assert.Equal(pendingId, client.UploadIds[0]);
        Assert.Equal(2, client.ReportCount);
    }

    /// <summary>
    /// Проверяет сохранение аренды браузерного пакета при неопределённом результате и удаление файлов после подтверждённого повтора.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task BrowserPackageLeaseSurvivesUnknownOutcomeAndIsDeletedAfterConfirmedRetry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mini-pdm-lease-{Guid.NewGuid():N}.a3d");
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        var client = new FakeClient { ImportOutcomeUnknown = true };
        using var viewModel = new ImportViewModel(client, () => Task.CompletedTask);
        var package = new SelectedImportPackage([path], new DeleteFileLease(path));

        await viewModel.ImportPackageAsync(package);

        Assert.True(File.Exists(path));
        Assert.NotNull(viewModel.PendingImportId);
        client.ReportAvailable = true;
        await viewModel.RetryCommand.ExecuteAsync();

        Assert.False(File.Exists(path));
        Assert.Null(viewModel.PendingImportId);
    }

    /// <summary>
    /// Ожидает выполнения условия или сообщает об истечении срока ожидания.
    /// </summary>
    /// <param name="condition">Условие завершения ожидания.</param>
    /// <returns>Завершение асинхронной операции.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var limit = DateTime.UtcNow.AddSeconds(3);
        while (!condition() && DateTime.UtcNow < limit)
            await Task.Delay(10);
        Assert.True(condition());
    }

    /// <summary>
    /// Создаёт краткие сведения об объекте для тестового сценария.
    /// </summary>
    /// <param name="id">Идентификатор объекта для чтения.</param>
    /// <param name="designation">Обозначение создаваемого объекта.</param>
    /// <param name="type">Тип создаваемого объекта.</param>
    /// <returns>Значение, сформированное для тестового сценария.</returns>
    private static ObjectSearchItemDto Item(Guid id, string designation, string type = "Part") =>
        new(id, type, designation, "Part", null, null, null, null, Guid.NewGuid(), true);

    /// <summary>
    /// Создаёт карточку объекта с заданными сведениями о версии.
    /// </summary>
    /// <param name="id">Идентификатор объекта для чтения.</param>
    /// <param name="name">Наименование создаваемой версии.</param>
    /// <param name="token">Значение token, используемое в этой проверке.</param>
    /// <param name="history">Значение history, используемое в этой проверке.</param>
    /// <param name="type">Тип создаваемого объекта.</param>
    /// <returns>Значение, сформированное для тестового сценария.</returns>
    private static ObjectCardDto Card(Guid id, string? name, Guid token, ObjectVersionSummaryDto? history = null, string type = "Part")
    {
        var selected = history is null ? Version(1, "InWork", name ?? "Part") : null;
        IReadOnlyList<ObjectVersionSummaryDto> summaries = history is null
            ? [new ObjectVersionSummaryDto(selected!.Id, 1, "InWork", true)] : [history];
        return new ObjectCardDto(id, type, "АБВГ.301245.001", name, null, token, selected,
            summaries, null, null);
    }

    /// <summary>
    /// Создаёт карточку объекта с заданными сведениями о версии.
    /// </summary>
    /// <param name="id">Идентификатор объекта для чтения.</param>
    /// <param name="name">Наименование создаваемой версии.</param>
    /// <param name="token">Значение token, используемое в этой проверке.</param>
    /// <param name="selected">Значение selected, используемое в этой проверке.</param>
    /// <param name="summaries">Значение summaries, используемое в этой проверке.</param>
    /// <returns>Значение, сформированное для тестового сценария.</returns>
    private static ObjectCardDto Card(Guid id, string? name, Guid token, ObjectVersionDto? selected,
        IReadOnlyList<ObjectVersionSummaryDto> summaries) =>
        new(id, "Part", "АБВГ.301245.001", name, null, token, selected, summaries, null, null);

    /// <summary>
    /// Создаёт краткие данные версии для тестового сценария.
    /// </summary>
    /// <param name="number">Номер создаваемой версии.</param>
    /// <param name="state">Состояние создаваемой версии.</param>
    /// <param name="name">Наименование создаваемой версии.</param>
    /// <returns>Значение, сформированное для тестового сценария.</returns>
    private static ObjectVersionDto Version(int number, string state, string? name)
    {
        var id = Guid.NewGuid();
        return new ObjectVersionDto(id, number, state, name, "Steel", 1m, null, false);
    }

    private sealed class DeleteFileLease(string path) : IDisposable
    {
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class FakeClient : IPdmApiClient
    {
        private int _cardCalls;
        /// <summary>
        /// Карточки объектов, возвращаемые тестовым клиентом.
        /// </summary>
        public Dictionary<Guid, ObjectCardDto> Cards { get; } = [];
        public Dictionary<(Guid, int), VersionCompositionDto> VersionCompositions { get; } = [];
        /// <summary>
        /// Фабрика карточек объектов для тестовых ответов.
        /// </summary>
        public Func<Guid, int?, int, ObjectCardDto>? CardFactory
        {
            get; set;
        }
        /// <summary>
        /// Переопределение получения карточки объекта в выбранном сценарии.
        /// </summary>
        public Func<Guid, int?, CancellationToken, Task<ObjectCardDto>>? GetObjectOverride
        {
            get; set;
        }
        /// <summary>
        /// Указывает, должен ли тестовый клиент возвращать конфликт обновления.
        /// </summary>
        public bool ConflictOnUpdate
        {
            get; set;
        }
        /// <summary>
        /// Указывает, должен ли результат импорта считаться неизвестным.
        /// </summary>
        public bool ImportOutcomeUnknown
        {
            get; set;
        }
        /// <summary>
        /// Указывает, доступен ли отчёт импорта при повторном запросе.
        /// </summary>
        public bool ReportAvailable
        {
            get; set;
        }
        /// <summary>
        /// Число вызовов обновления атрибутов.
        /// </summary>
        public int UpdateCount
        {
            get; private set;
        }
        /// <summary>
        /// Число запросов отчёта импорта.
        /// </summary>
        public int ReportCount
        {
            get; private set;
        }
        /// <summary>
        /// Идентификаторы пакетов, переданных при импорте.
        /// </summary>
        public List<Guid> UploadIds { get; } = [];
        /// <summary>
        /// Пути файлов, переданных тестовым клиенту.
        /// </summary>
        public List<string[]> UploadedFiles { get; } = [];

        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="search">Значение search, используемое в этой проверке.</param>
        /// <param name="offset">Значение offset, используемое в этой проверке.</param>
        /// <param name="limit">Значение limit, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ObjectSearchPageDto> SearchObjectsAsync(string? search = null, int offset = 0, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ObjectSearchPageDto([], offset, limit, false));
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ObjectCardDto> GetObjectAsync(Guid objectId, int? version = null, CancellationToken cancellationToken = default)
        {
            var number = Interlocked.Increment(ref _cardCalls);
            if (GetObjectOverride is not null)
                return GetObjectOverride(objectId, version, cancellationToken);
            if (CardFactory is not null)
            {
                var generated = CardFactory(objectId, version, number);
                Cards[objectId] = generated;
                return Task.FromResult(generated);
            }
            if (!Cards.TryGetValue(objectId, out var card))
                return Task.FromResult(Card(objectId, "Part", Guid.NewGuid()));
            if (version is null)
                return Task.FromResult(card);
            var selectedSummary = card.Versions.Single(x => x.Version == version);
            var selected = selectedSummary.Version == card.SelectedVersion?.Version
                ? card.SelectedVersion
                : new ObjectVersionDto(selectedSummary.Id, selectedSummary.Version, selectedSummary.State,
                    card.Type == "Assembly" ? null : "Restorable version",
                    card.Type == "Assembly" ? null : "Steel",
                    card.Type == "Assembly" ? null : 1m, null, selectedSummary.IsCurrent);
            return Task.FromResult(new ObjectCardDto(card.Id, card.Type, card.Designation, card.Name, card.CurrentVersionId,
                card.ConcurrencyToken, selected, card.Versions, card.ErrorCode, card.Error));
        }
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<CompositionTreeDto> GetCompositionAsync(Guid objectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompositionTreeDto(objectId, []));
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionCompositionDto> GetVersionCompositionAsync(Guid objectId, int version, CancellationToken cancellationToken = default) =>
            Task.FromResult(VersionCompositions.GetValueOrDefault((objectId, version)) ?? new VersionCompositionDto(objectId, version,
                Cards.GetValueOrDefault(objectId)?.ConcurrencyToken ?? Guid.Empty, []));
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<CompositionCalculationDto> GetCalculationAsync(Guid objectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompositionCalculationDto(objectId, null, false, [], []));
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="importId">Значение importId, используемое в этой проверке.</param>
        /// <param name="filePaths">Значение filePaths, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ImportReportDto> ImportFilesAsync(Guid importId, IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default)
        {
            UploadIds.Add(importId);
            UploadedFiles.Add(filePaths.ToArray());
            if (ImportOutcomeUnknown)
                throw new PdmApiException(503, "Unknown upload outcome.");
            return Task.FromResult(new ImportReportDto(importId, []));
        }
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="importId">Значение importId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ImportReportDto> GetImportReportAsync(Guid importId, CancellationToken cancellationToken = default)
        {
            ReportCount++;
            if (!ReportAvailable)
                throw new PdmApiException(404, "Report not found.");
            return Task.FromResult(new ImportReportDto(importId, []));
        }
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionMutationDto> CloneVersionAsync(Guid objectId, CloneVersionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionMutationDto> ChangeVersionStateAsync(Guid objectId, int version, ChangeVersionStateRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionMutationDto> UpdateVersionAttributesAsync(Guid objectId, int version, UpdateVersionAttributesRequestDto request, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            if (ConflictOnUpdate)
                throw new PdmApiException(409, "The object was changed concurrently.");
            return Task.FromResult(new VersionMutationDto(objectId, Guid.NewGuid(), version, "InWork", null, Guid.NewGuid(), []));
        }
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionMutationDto> ReplaceCompositionAsync(Guid objectId, int version, ReplaceCompositionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<IReadOnlyList<BackgroundTaskDto>> GetBackgroundTasksAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BackgroundTaskDto>>([]);
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="taskId">Значение taskId, используемое в этой проверке.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<BackgroundTaskDto> UpdateBackgroundTaskScheduleAsync(string taskId, UpdateBackgroundTaskScheduleRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="taskId">Значение taskId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<BackgroundTaskRunAcceptedDto> RunBackgroundTaskAsync(string taskId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
