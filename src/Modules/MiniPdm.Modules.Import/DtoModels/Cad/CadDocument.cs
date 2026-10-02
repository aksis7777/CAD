using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Нормализованные сведения о CAD-документе и его составе.
/// </summary>
/// <param name="FileName">Имя исходного CAD-файла.</param>
/// <param name="Type">Вид PDM-объекта, создаваемого из документа.</param>
/// <param name="Designation">Обозначение документа, если задано.</param>
/// <param name="Name">Наименование объекта.</param>
/// <param name="Material">Материал, если указан.</param>
/// <param name="Mass">Масса, если указана.</param>
/// <param name="Components">Ссылки на файлы компонентов и их количества.</param>
public sealed record CadDocument(
    string FileName,
    PdmObjectType Type,
    string? Designation,
    string Name,
    string? Material,
    decimal? Mass,
    IReadOnlyList<CadComponent> Components);

/// <summary>
/// Позиция состава CAD-документа, ссылающаяся на другой файл пакета.
/// </summary>
/// <param name="File">Имя файла компонента.</param>
/// <param name="Count">Количество экземпляров компонента.</param>
public sealed record CadComponent(string File, int Count);
