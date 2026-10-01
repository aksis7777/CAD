namespace MiniPdm.Storage.Abstractions.Objects;

public interface IObjectReadQuery
{
    /// <summary>Searches literal case-insensitive substrings in identity text and the valid current version name, using stable order and limit-plus-one pagination.</summary>
    Task<ObjectSearchPage> SearchAsync(string search, int offset, int limit, CancellationToken cancellationToken);

    /// <summary>Reads object metadata and version summaries with one projected read; only the requested version (or the valid current version by default) includes full attributes.</summary>
    /// <remarks>Historical cancelled versions may be requested explicitly. A missing object returns null; an absent requested version has a null selected version.</remarks>
    Task<ObjectCardReadRow?> GetAsync(Guid objectId, int? versionNumber, CancellationToken cancellationToken);
}
