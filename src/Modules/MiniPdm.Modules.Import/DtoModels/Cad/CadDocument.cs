using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Нормализованные сведения о CAD-документе и его составе.
/// </summary>
public sealed record CadDocument(
    string FileName,
    PdmObjectType Type,
    string? Designation,
    string Name,
    string? Material,
    decimal? Mass,
    IReadOnlyList<CadComponent> Components)
{
    /// <summary>
    /// Имя исходного CAD-файла.
    /// </summary>
    public string FileName { get; init; } = FileName;

    /// <summary>
    /// Вид PDM-объекта, создаваемого из документа.
    /// </summary>
    public PdmObjectType Type { get; init; } = Type;

    /// <summary>
    /// Обозначение документа, если оно задано.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Наименование объекта.
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Материал объекта, если он указан.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Масса объекта, если она указана.
    /// </summary>
    public decimal? Mass { get; init; } = Mass;

    /// <summary>
    /// Ссылки на файлы компонентов и их количества.
    /// </summary>
    public IReadOnlyList<CadComponent> Components { get; init; } = Components;
}

/// <summary>
/// Позиция состава CAD-документа, ссылающаяся на другой файл пакета.
/// </summary>
public sealed record CadComponent(string File, int Count)
{
    /// <summary>
    /// Имя файла компонента.
    /// </summary>
    public string File { get; init; } = File;

    /// <summary>
    /// Количество экземпляров компонента.
    /// </summary>
    public int Count { get; init; } = Count;
}
