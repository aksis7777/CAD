namespace MiniPdm.Storage.Abstractions.Composition;

public interface IVersionCompositionReadQuery
{
    Task<VersionCompositionReadRow?> ReadAsync(Guid objectId, int version, CancellationToken cancellationToken);
}
