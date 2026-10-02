namespace MiniPdm.Modules.BackgroundTasks.Abstractions;

/// <summary>
/// Содержит итоговое сообщение и ошибки одного выполнения фоновой задачи.
/// </summary>
/// <param name="Summary">Краткое описание результата выполнения.</param>
/// <param name="Errors">Ошибки, возникшие при обработке; пустой список означает выполнение без ошибок.</param>
public sealed record BackgroundTaskExecutionResult(string Summary, IReadOnlyList<string> Errors);

/// <summary>
/// Описывает фоновую задачу, которую координатор регистрирует и запускает по расписанию или вручную.
/// </summary>
/// <param name="Id">Уникальный идентификатор задачи.</param>
/// <param name="Name">Отображаемое имя задачи.</param>
/// <param name="DefaultIntervalMinutes">Интервал запуска при первоначальной регистрации, в минутах.</param>
/// <param name="ExecuteAsync">Делегат выполнения, получающий область служб и токен отмены.</param>
public sealed record BackgroundTaskDefinition(
    string Id,
    string Name,
    int DefaultIntervalMinutes,
    Func<IServiceProvider, CancellationToken, Task<BackgroundTaskExecutionResult>> ExecuteAsync);

/// <summary>
/// Задаёт результат запроса на ручной запуск фоновой задачи.
/// </summary>
public enum BackgroundTaskRunRequestStatus
{
    /// <summary>
    /// Запуск принят.
    /// </summary>
    Accepted,

    /// <summary>
    /// Задача с указанным идентификатором не зарегистрирована.
    /// </summary>
    NotFound,

    /// <summary>
    /// Задача уже выполняется.
    /// </summary>
    Running
}

/// <summary>
/// Содержит статус запроса на ручной запуск.
/// </summary>
/// <param name="Status">Статус принятия запроса.</param>
public sealed record BackgroundTaskRunRequestResult(BackgroundTaskRunRequestStatus Status);

/// <summary>
/// Предоставляет операции чтения, настройки и ручного запуска фоновых задач.
/// </summary>
public interface IBackgroundTaskCoordinator
{
    /// <summary>
    /// Возвращает зарегистрированные задачи с их сохранённым состоянием.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список сведений о задачах.</returns>
    Task<IReadOnlyList<MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels.BackgroundTaskDto>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Изменяет интервал запуска зарегистрированной задачи.
    /// </summary>
    /// <param name="taskId">Идентификатор задачи.</param>
    /// <param name="intervalMinutes">Новый интервал в минутах.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Обновлённые сведения о задаче либо <see langword="null"/>, если задача не зарегистрирована.</returns>
    Task<MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels.BackgroundTaskDto?> UpdateScheduleAsync(string taskId, int intervalMinutes, CancellationToken ct);

    /// <summary>
    /// Запрашивает немедленный ручной запуск зарегистрированной задачи.
    /// </summary>
    /// <param name="taskId">Идентификатор задачи.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Статус принятия запроса, включая отсутствие задачи или уже выполняющийся запуск.</returns>
    Task<BackgroundTaskRunRequestResult> RequestManualRunAsync(string taskId, CancellationToken ct);
}

/// <summary>
/// Указывает, что хранилище состояния фоновых задач временно недоступно.
/// </summary>
/// <param name="message">Сообщение об ошибке.</param>
/// <param name="innerException">Исходное исключение операции хранилища.</param>
public sealed class BackgroundTaskPersistenceUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
