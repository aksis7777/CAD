namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Описывает способ доступа к CAD-источнику и его расположение.
/// </summary>
/// <param name="Kind">Ключ адаптера источника.</param>
/// <param name="Location">Путь или иной адрес источника.</param>
public sealed record CadSourceDescriptor(string Kind, string Location);
