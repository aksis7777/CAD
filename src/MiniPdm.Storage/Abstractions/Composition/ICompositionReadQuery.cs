namespace MiniPdm.Storage.Abstractions.Composition;

public interface ICompositionReadQuery
{
    /// <summary>Reads all occurrences in one recursive SQL statement, observing one database statement snapshot.</summary>
    /// <remarks>Returns an empty list when the root object does not exist. A cycle is emitted once as a leaf occurrence.</remarks>
    Task<IReadOnlyList<CompositionOccurrence>> ReadAsync(Guid rootObjectId, CancellationToken cancellationToken);
}
