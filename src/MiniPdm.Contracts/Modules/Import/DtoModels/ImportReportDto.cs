namespace MiniPdm.Contracts.Modules.Import.DtoModels;

public enum ImportFileStatus { Accepted, Rejected }
public enum ImportFileAction { Created, Updated, NewVersion, Unchanged }

public sealed record ImportFileResultDto(
    string FileName,
    ImportFileStatus Status,
    string? Reason,
    ImportFileAction? Action,
    IReadOnlyList<string> Warnings);

public sealed record ImportReportDto(Guid ImportId, IReadOnlyList<ImportFileResultDto> Files)
{
    public int AcceptedCount => Files.Count(x => x.Status == ImportFileStatus.Accepted);
    public int RejectedCount => Files.Count(x => x.Status == ImportFileStatus.Rejected);
    public int WarningCount => Files.Count(x => x.Status == ImportFileStatus.Accepted && x.Warnings.Count > 0);
}
