using MiniPdm.Domain.Versions;

namespace MiniPdm.Domain.Objects;

public enum PdmObjectType { Assembly = 1, Part = 2, StandardPart = 3 }

public sealed class PdmObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PdmObjectType Type { get; set; }
    public string? Designation { get; set; }
    public string? NormalizedName { get; set; }
    public string? StandardName { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public ObjectVersion? CurrentVersion { get; set; }
    public ICollection<ObjectVersion> Versions { get; set; } = new List<ObjectVersion>();
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
