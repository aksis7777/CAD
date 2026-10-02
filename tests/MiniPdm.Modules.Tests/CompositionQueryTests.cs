using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;
using Xunit;

namespace MiniPdm.Modules.Tests;

public sealed class CompositionQueryTests
{
    [Fact]
    public void MapsDistinctOccurrencePathsInDiamond()
    {
        var root = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var leaf = Guid.NewGuid();
        var occurrences = new CompositionOccurrence[]
        {
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(left, [root, left], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(right, [root, right], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(leaf, [root, left, leaf], [root, left], PdmObjectType.Part, Guid.NewGuid()),
            Occurrence(leaf, [root, right, leaf], [root, right], PdmObjectType.Part, Guid.NewGuid())
        };
        var tree = CompositionReadService.MapTreeDto(root, occurrences);
        Assert.Equal(root, tree.RootObjectId);
        var leafNodes = tree.Nodes.Where(node => node.ObjectId == leaf).ToArray();
        Assert.Equal(2, leafNodes.Length);
        Assert.NotEqual(leafNodes[0].ObjectPath, leafNodes[1].ObjectPath);
        Assert.Equal(new[] { root, left, leaf }, leafNodes[0].ObjectPath);
        Assert.Equal(new[] { root, right, leaf }, leafNodes[1].ObjectPath);
        Assert.All(leafNodes, node => Assert.Equal("Part", node.Type));
    }

    [Fact]
    public void MapsRootOnlyComposition()
    {
        var id = Guid.NewGuid();
        var tree = CompositionReadService.MapTreeDto(id, [Occurrence(id, [id], null, PdmObjectType.Part, Guid.NewGuid())]);
        Assert.Equal(id, tree.RootObjectId);
        Assert.Single(tree.Nodes);
    }

    [Fact]
    public void MapsNodeWithoutCurrentVersionWithDiagnostic()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var query = new CompositionOccurrence[]
        {
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(child, [root, child], [root], PdmObjectType.Part, null, name: "Missing version")
        };

        var tree = CompositionReadService.MapTreeDto(root, query);
        var node = Assert.Single(tree.Nodes, node => node.ObjectId == child);
        Assert.Equal("NoCurrentVersion", node.ErrorCode);
        Assert.NotNull(node.Error);
        Assert.Null(node.VersionId);
        Assert.Equal("Missing version", node.Name);
    }

    [Fact]
    public void MapsCycleWithTheOccurrencePath()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var cyclePath = new[] { root, child, root };
        var query = new CompositionOccurrence[]
        {
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(child, [root, child], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(root, cyclePath, [root, child], PdmObjectType.Assembly, Guid.NewGuid(), isCycle: true)
        };

        var tree = CompositionReadService.MapTreeDto(root, query);
        var cycle = Assert.Single(tree.Nodes, node => node.ErrorCode == "Cycle");
        Assert.Equal(cyclePath, cycle.ObjectPath);
        Assert.Contains(string.Join(" → ", cyclePath), cycle.Error!);
    }

    private static CompositionOccurrence Occurrence(
        Guid id,
        Guid[] path,
        Guid[]? parentPath,
        PdmObjectType type,
        Guid? versionId,
        bool isCycle = false,
        string? name = "Sample") => new(
            id,
            path,
            parentPath,
            1,
            type,
            type == PdmObjectType.StandardPart ? null : "АБВГ.301245.001",
            name,
            "Steel",
            versionId,
            versionId is null ? null : 1,
            versionId is null ? null : VersionState.InWork,
            type == PdmObjectType.Assembly ? null : 2.5m,
            isCycle);

}
