using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Import.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Contracts.Modules.Versions.DtoModels;
using MiniPdm.Desktop.Services;
using MiniPdm.Desktop.Services.Abstractions;
using MiniPdm.Desktop.Modules.BackgroundTasks.ViewModels;
using Xunit;

namespace MiniPdm.Desktop.Tests;

/// <summary>
/// Проверяет команды и отображение расписания фоновых задач в модели представления.
/// </summary>
public sealed class BackgroundTasksViewModelTests
{
    /// <summary>
    /// Проверяет отклонение недопустимого интервала и сохранение корректного расписания фоновой задачи.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task ScheduleRejectsInvalidIntervalAndSavesValidInterval()
    {
        var client = new FakeClient();
        var viewModel = new BackgroundTasksViewModel(client, TimeSpan.Zero, maxPollCount: 0);
        await WaitUntilAsync(() => viewModel.Tasks.Count == 1 && !viewModel.IsBusy);

        viewModel.IntervalMinutesText = "0";
        Assert.False(viewModel.SaveScheduleCommand.CanExecute(null));
        viewModel.IntervalMinutesText = "60";
        Assert.True(viewModel.SaveScheduleCommand.CanExecute(null));
        await viewModel.SaveScheduleCommand.ExecuteAsync();

        Assert.Equal(60, client.SavedInterval);
    }

    /// <summary>
    /// Проверяет, что команда запуска не допускает одновременного выполнения одной фоновой задачи.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task RunCommandPreventsOverlappingRuns()
    {
        var client = new FakeClient { BlockRun = true };
        var viewModel = new BackgroundTasksViewModel(client, TimeSpan.Zero, maxPollCount: 0);
        await WaitUntilAsync(() => viewModel.Tasks.Count == 1 && !viewModel.IsBusy);

        var first = viewModel.RunCommand.ExecuteAsync();
        await client.RunEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(viewModel.RunCommand.CanExecute(null));
        var second = viewModel.RunCommand.ExecuteAsync();
        client.ReleaseRun.TrySetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, client.RunCount);
    }

    /// <summary>
    /// Проверяет отображение оператору конфликта при изменении расписания.
    /// </summary>
    /// <returns>Завершение проверки подтверждает ожидаемое поведение; нарушение ожиданий приводит к ошибке утверждения.</returns>
    [Fact]
    public async Task ScheduleConflictIsShownToTheOperator()
    {
        var client = new FakeClient { ScheduleConflict = true };
        var viewModel = new BackgroundTasksViewModel(client, TimeSpan.Zero, maxPollCount: 0);
        await WaitUntilAsync(() => viewModel.Tasks.Count == 1 && !viewModel.IsBusy);
        viewModel.IntervalMinutesText = "30";

        await viewModel.SaveScheduleCommand.ExecuteAsync();

        Assert.Contains("schedule changed", viewModel.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ожидает выполнения условия или сообщает об истечении срока ожидания.
    /// </summary>
    /// <param name="condition">Условие завершения ожидания.</param>
    /// <returns>Завершение асинхронной операции.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stop = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < stop)
            await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class FakeClient : IPdmApiClient
    {
        private BackgroundTaskDto _task = new("cleanup", "Cleanup", 1440, "Idle", null, null, null, null, null);
        /// <summary>
        /// Последний сохранённый интервал запуска в минутах.
        /// </summary>
        public int? SavedInterval
        {
            get; private set;
        }
        /// <summary>
        /// Указывает, следует ли удерживать запуск задачи до сигнала теста.
        /// </summary>
        public bool BlockRun
        {
            get; init;
        }
        /// <summary>
        /// Указывает, должен ли тестовый клиент сообщать о конфликте расписания.
        /// </summary>
        public bool ScheduleConflict
        {
            get; init;
        }
        /// <summary>
        /// Число запусков фоновой задачи через тестовый клиент.
        /// </summary>
        public int RunCount
        {
            get; private set;
        }
        /// <summary>
        /// Сигнализирует, что выполнение фоновой задачи началось.
        /// </summary>
        public TaskCompletionSource RunEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>
        /// Разрешает завершение удерживаемого выполнения фоновой задачи.
        /// </summary>
        public TaskCompletionSource ReleaseRun { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="search">Значение search, используемое в этой проверке.</param>
        /// <param name="offset">Значение offset, используемое в этой проверке.</param>
        /// <param name="limit">Значение limit, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ObjectSearchPageDto> SearchObjectsAsync(string? search = null, int offset = 0, int limit = 50, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ObjectCardDto> GetObjectAsync(Guid objectId, int? version = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<CompositionTreeDto> GetCompositionAsync(Guid objectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="version">Версия, назначаемая текущей для объекта.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<VersionCompositionDto> GetVersionCompositionAsync(Guid objectId, int version, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="objectId">Значение objectId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<CompositionCalculationDto> GetCalculationAsync(Guid objectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="importId">Значение importId, используемое в этой проверке.</param>
        /// <param name="filePaths">Значение filePaths, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ImportReportDto> ImportFilesAsync(Guid importId, IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="importId">Значение importId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<ImportReportDto> GetImportReportAsync(Guid importId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
        public Task<VersionMutationDto> UpdateVersionAttributesAsync(Guid objectId, int version, UpdateVersionAttributesRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
        public Task<IReadOnlyList<BackgroundTaskDto>> GetBackgroundTasksAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BackgroundTaskDto>>([_task]);
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="taskId">Значение taskId, используемое в этой проверке.</param>
        /// <param name="request">HTTP-запрос, отправленный тестовым клиентом.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public Task<BackgroundTaskDto> UpdateBackgroundTaskScheduleAsync(string taskId, UpdateBackgroundTaskScheduleRequestDto request, CancellationToken cancellationToken = default)
        {
            if (ScheduleConflict)
                throw new PdmApiException(409, "The schedule changed on the server.");
            SavedInterval = request.IntervalMinutes;
            _task = _task with
            {
                IntervalMinutes = request.IntervalMinutes
            };
            return Task.FromResult(_task);
        }
        /// <summary>
        /// Реализует операцию тестового помощника.
        /// </summary>
        /// <param name="taskId">Значение taskId, используемое в этой проверке.</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции.</param>
        /// <returns>Задача, завершающая тестовую операцию и предоставляющая её результат.</returns>
        public async Task<BackgroundTaskRunAcceptedDto> RunBackgroundTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            RunCount++;
            RunEntered.TrySetResult();
            if (BlockRun)
                await ReleaseRun.Task;
            return new BackgroundTaskRunAcceptedDto(taskId);
        }
    }
}
