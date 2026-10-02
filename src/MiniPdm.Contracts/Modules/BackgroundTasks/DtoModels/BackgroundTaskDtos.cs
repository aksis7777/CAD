using System.ComponentModel.DataAnnotations;

namespace MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

/// <summary>
/// Представляет сведения о состоянии, расписании и последнем выполнении фоновой задачи в API.
/// </summary>
public sealed record BackgroundTaskDto(
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
    /// Уникальный идентификатор задачи.
    /// </summary>
    public string Id { get; init; } = Id;

    /// <summary>
    /// Отображаемое имя задачи.
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Текущий интервал запуска в минутах.
    /// </summary>
    public int IntervalMinutes { get; init; } = IntervalMinutes;

    /// <summary>
    /// Текущее состояние задачи.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Время следующего запуска либо <see langword="null"/>, если он не запланирован.
    /// </summary>
    public DateTimeOffset? NextRunAt { get; init; } = NextRunAt;

    /// <summary>
    /// Время последнего начала либо <see langword="null"/>, если задача ещё не запускалась.
    /// </summary>
    public DateTimeOffset? LastStartedAt { get; init; } = LastStartedAt;

    /// <summary>
    /// Время последнего завершения либо <see langword="null"/>, если выполнение ещё не завершалось.
    /// </summary>
    public DateTimeOffset? LastCompletedAt { get; init; } = LastCompletedAt;

    /// <summary>
    /// Краткий результат последнего выполнения либо <see langword="null"/>.
    /// </summary>
    public string? LastResult { get; init; } = LastResult;

    /// <summary>
    /// Описание ошибки последнего выполнения либо <see langword="null"/>, если ошибки нет.
    /// </summary>
    public string? LastError { get; init; } = LastError;

}

/// <summary>
/// Содержит интервал запуска задачи, передаваемый при изменении расписания.
/// </summary>
public sealed record UpdateBackgroundTaskScheduleRequestDto([Range(1, 525600)] int IntervalMinutes)
{
    /// <summary>
    /// Новый интервал запуска в минутах; допустимый диапазон — от 1 до 525600.
    /// </summary>
    public int IntervalMinutes { get; init; } = IntervalMinutes;

}

/// <summary>
/// Подтверждает принятие запроса на ручной запуск фоновой задачи.
/// </summary>
public sealed record BackgroundTaskRunAcceptedDto(string TaskId)
{
    /// <summary>
    /// Идентификатор задачи, для которой принят запуск.
    /// </summary>
    public string TaskId { get; init; } = TaskId;

}
