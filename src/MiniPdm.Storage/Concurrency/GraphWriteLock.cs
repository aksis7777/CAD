namespace MiniPdm.Storage.Concurrency;

/// <summary>Transaction advisory lock shared by every command that changes the active BOM graph.</summary>
public static class GraphWriteLock
{
    public const long AdvisoryLockKey = 0x50444D4752415048;
}
