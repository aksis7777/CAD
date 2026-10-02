namespace MiniPdm.Modules.BackgroundTasks.DtoModels;

/// <summary>
/// Описывает зарегистрированную фоновую задачу и её интервал запуска по умолчанию.
/// </summary>
public sealed record BackgroundTaskDefinitionRecord(
    string Id,
    string Name,
    int DefaultIntervalMinutes)
{
    /// <summary>
    /// Уникальный идентификатор фоновой задачи.
    /// </summary>
    public string Id { get; init; } = Id;

    /// <summary>
    /// Отображаемое имя фоновой задачи.
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Интервал запуска задачи по умолчанию в минутах.
    /// </summary>
    public int DefaultIntervalMinutes { get; init; } = DefaultIntervalMinutes;
}

/// <summary>
/// Содержит сохранённое состояние фоновой задачи для отображения и планирования запусков.
/// </summary>
public sealed record BackgroundTaskRow(
    string Id,
    string Name,
    int IntervalMinutes,
    string State,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastResult,
    string? LastError)
{
    /// <summary>
    /// Уникальный идентификатор фоновой задачи.
    /// </summary>
    public string Id { get; init; } = Id;

    /// <summary>
    /// Отображаемое имя фоновой задачи.
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Текущий интервал запуска задачи в минутах.
    /// </summary>
    public int IntervalMinutes { get; init; } = IntervalMinutes;

    /// <summary>
    /// Текущее состояние фоновой задачи.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Время следующего запланированного запуска или <see langword="null"/>, если запуск не запланирован.
    /// </summary>
    public DateTimeOffset? NextRunAt { get; init; } = NextRunAt;

    /// <summary>
    /// Время последнего начала выполнения или <see langword="null"/>, если задача ещё не запускалась.
    /// </summary>
    public DateTimeOffset? LastStartedAt { get; init; } = LastStartedAt;

    /// <summary>
    /// Время последнего завершения выполнения или <see langword="null"/>, если выполнение ещё не завершалось.
    /// </summary>
    public DateTimeOffset? LastCompletedAt { get; init; } = LastCompletedAt;

    /// <summary>
    /// Результат последнего выполнения или <see langword="null"/>, если результата нет.
    /// </summary>
    public string? LastResult { get; init; } = LastResult;

    /// <summary>
    /// Описание последней ошибки или <see langword="null"/>, если ошибки нет.
    /// </summary>
    public string? LastError { get; init; } = LastError;
}
