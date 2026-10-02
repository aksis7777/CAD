namespace MiniPdm.Modules.BackgroundTasks.DtoModels;

/// <summary>
/// Описывает зарегистрированную фоновую задачу и её интервал запуска по умолчанию.
/// </summary>
/// <param name="Id">Уникальный идентификатор фоновой задачи.</param>
/// <param name="Name">Отображаемое имя фоновой задачи.</param>
/// <param name="DefaultIntervalMinutes">Интервал запуска задачи по умолчанию в минутах.</param>
public sealed record BackgroundTaskDefinitionRecord(
    string Id,
    string Name,
    int DefaultIntervalMinutes);

/// <summary>
/// Содержит сохранённое состояние фоновой задачи для отображения и планирования запусков.
/// </summary>
/// <param name="Id">Уникальный идентификатор фоновой задачи.</param>
/// <param name="Name">Отображаемое имя фоновой задачи.</param>
/// <param name="IntervalMinutes">Текущий интервал запуска задачи в минутах.</param>
/// <param name="State">Текущее состояние фоновой задачи.</param>
/// <param name="NextRunAt">Время следующего запланированного запуска или <see langword="null"/>, если запуск не запланирован.</param>
/// <param name="LastStartedAt">Время последнего начала выполнения или <see langword="null"/>, если задача ещё не запускалась.</param>
/// <param name="LastCompletedAt">Время последнего завершения выполнения или <see langword="null"/>, если выполнение ещё не завершалось.</param>
/// <param name="LastResult">Результат последнего выполнения или <see langword="null"/>, если результата нет.</param>
/// <param name="LastError">Описание последней ошибки или <see langword="null"/>, если ошибки нет.</param>
public sealed record BackgroundTaskRow(
    string Id,
    string Name,
    int IntervalMinutes,
    string State,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastResult,
    string? LastError);
