using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Domain.Composition;

public sealed class BomLink
{
    public Guid ParentVersionId { get; set; }
    public Guid ChildObjectId { get; set; }
    public int Quantity { get; set; }
    public ObjectVersion? ParentVersion { get; set; }
    public PdmObject? ChildObject { get; set; }
}
