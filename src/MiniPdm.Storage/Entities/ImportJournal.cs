namespace MiniPdm.Storage.Entities;

public sealed class ImportJournal
{
    public Guid ImportId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public string ReportJson { get; set; } = "";
}
