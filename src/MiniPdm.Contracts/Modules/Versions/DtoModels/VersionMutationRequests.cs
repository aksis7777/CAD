namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>
/// Запрашивает создание рабочей версии на основе выбранной версии объекта.
/// Существующая история версий при этом сохраняется.
/// </summary>
/// <param name="SourceVersion">Номер версии, используемой как источник копирования.</param>
/// <param name="ExpectedConcurrencyToken">Токен конкурентного доступа объекта, полученный при чтении.</param>
public sealed record CloneVersionRequestDto(
    int SourceVersion,
    Guid ExpectedConcurrencyToken);

/// <summary>
/// Запрашивает изменение состояния версии.
/// </summary>
/// <param name="State">Новое состояние версии.</param>
/// <param name="ExpectedConcurrencyToken">Токен конкурентного доступа объекта, полученный при чтении.</param>
public sealed record ChangeVersionStateRequestDto(
    string State,
    Guid ExpectedConcurrencyToken);
