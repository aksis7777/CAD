namespace MiniPdm.Contracts.Modules.Objects.DtoModels;

/// <summary>
/// Содержит значения атрибутов версии, которые нужно обновить.
/// Токен позволяет проверить, что версия не изменилась после её чтения.
/// </summary>
/// <param name="Name">Новое наименование версии или <see langword="null"/>, если наименование менять не нужно.</param>
/// <param name="Material">Новый материал версии или <see langword="null"/>, если материал менять не нужно.</param>
/// <param name="Mass">Новая масса единицы в килограммах или <see langword="null"/>, если массу менять не нужно.</param>
/// <param name="ExpectedConcurrencyToken">Токен конкурентного доступа, полученный при чтении версии.</param>
public sealed record UpdateVersionAttributesRequestDto(
    string? Name,
    string? Material,
    decimal? Mass,
    Guid ExpectedConcurrencyToken);
