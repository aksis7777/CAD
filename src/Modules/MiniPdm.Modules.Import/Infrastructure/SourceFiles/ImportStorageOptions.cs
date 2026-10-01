namespace MiniPdm.Modules.Import.Infrastructure.SourceFiles;

public sealed class ImportStorageOptions
{
    public const int HardMaxFiles = 1000;
    public const long HardMaxFileBytes = 8 * 1024 * 1024;
    public const long HardMaxTotalBytes = 64 * 1024 * 1024;
    public string? DataRoot { get; set; }
    public int MaxFiles { get; set; } = HardMaxFiles;
    public long MaxFileBytes { get; set; } = HardMaxFileBytes;
    public long MaxTotalBytes { get; set; } = HardMaxTotalBytes;
}
