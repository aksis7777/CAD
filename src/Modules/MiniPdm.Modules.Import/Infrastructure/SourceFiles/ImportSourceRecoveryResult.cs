namespace MiniPdm.Modules.Import.Infrastructure.SourceFiles;

/// <summary>
/// Итог сверки каталогов источников импорта с журналом базы данных.
/// </summary>
/// <param name="RemovedCount">Число удалённых заброшенных каталогов.</param>
/// <param name="Errors">Ошибки, из-за которых отдельные каталоги были оставлены.</param>
public sealed record ImportSourceRecoveryResult(int RemovedCount, IReadOnlyList<string> Errors);
