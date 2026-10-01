using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Composition;

namespace MiniPdm.Domain.Versions;

public enum VersionState { InWork = 1, Approved = 2, Cancelled = 3 }

public sealed class ObjectVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ObjectId { get; set; }
    public PdmObject? Object { get; set; }
    public ICollection<BomLink> Components { get; set; } = new List<BomLink>();
    public int Version { get; set; }
    public VersionState State { get; set; } = VersionState.InWork;
    public string? Name { get; set; }
    public string? Material { get; set; }
    public decimal? Mass { get; set; }
    public string? SourceReference { get; set; }

    public static bool CanTransition(VersionState from, VersionState to) =>
        (from, to) is (VersionState.InWork, VersionState.Approved)
            or (VersionState.InWork, VersionState.Cancelled)
            or (VersionState.Approved, VersionState.Cancelled);
}
