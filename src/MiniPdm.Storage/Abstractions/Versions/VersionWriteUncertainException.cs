namespace MiniPdm.Storage.Abstractions.Versions;

/// <summary>The database commit call failed after it began, so the caller must not retry automatically.</summary>
public sealed class VersionWriteUncertainException(string message, Exception innerException) : Exception(message, innerException);
