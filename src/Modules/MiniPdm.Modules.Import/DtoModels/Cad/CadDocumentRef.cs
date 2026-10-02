namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Ссылка на CAD-документ внутри источника.
/// </summary>
/// <param name="FileName">Имя файла документа.</param>
public sealed record CadDocumentRef(string FileName);
