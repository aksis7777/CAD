using System.ComponentModel.DataAnnotations;

namespace MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

/// <summary>
/// Представляет сведения о состоянии, расписании и последнем выполнении фоновой задачи в API.
/// </summary>
/// <param name="Id">Уникальный идентификатор задачи.</param>
/// <param name="Name">Отображаемое имя задачи.</param>
/// <param name="IntervalMinutes">Текущий интервал запуска в минутах.</param>
/// <param name="State">Текущее состояние задачи.</param>
/// <param name="NextRunAt">Время следующего запуска либо <see langword="null"/>, если он не запланирован.</param>
/// <param name="LastStartedAt">Время последнего начала либо <see langword="null"/>, если задача ещё не запускалась.</param>
/// <param name="LastCompletedAt">Время последнего завершения либо <see langword="null"/>, если выполнение ещё не завершалось.</param>
/// <param name="LastResult">Краткий результат последнего выполнения либо <see langword="null"/>.</param>
/// <param name="LastError">Описание ошибки последнего выполнения либо <see langword="null"/>, если ошибки нет.</param>
public sealed record BackgroundTaskDto(
    string Id,
    string Name,
    int IntervalMinutes,
    string State,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastResult,
    string? LastError);

/// <summary>
/// Содержит интервал запуска задачи, передаваемый при изменении расписания.
/// </summary>
/// <param name="IntervalMinutes">Новый интервал запуска в минутах; допустимый диапазон — от 1 до 525600.</param>
public sealed record UpdateBackgroundTaskScheduleRequestDto([Range(1, 525600)] int IntervalMinutes);

/// <summary>
/// Подтверждает принятие запроса на ручной запуск фоновой задачи.
/// </summary>
/// <param name="TaskId">Идентификатор задачи, для которой принят запуск.</param>
public sealed record BackgroundTaskRunAcceptedDto(string TaskId);
