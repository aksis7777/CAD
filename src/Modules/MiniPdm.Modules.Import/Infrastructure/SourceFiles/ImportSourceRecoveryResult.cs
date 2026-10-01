namespace MiniPdm.Modules.Import.Infrastructure.SourceFiles;

public sealed record ImportSourceRecoveryResult(int RemovedCount, IReadOnlyList<string> Errors);
