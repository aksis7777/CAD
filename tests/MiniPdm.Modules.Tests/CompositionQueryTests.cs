using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Composition.Features.GetComposition;
using MiniPdm.Storage.Abstractions.Composition;
using Xunit;

namespace MiniPdm.Modules.Tests;

public sealed class CompositionQueryTests
{
    [Fact]
    public async Task Handle_PreservesDistinctOccurrencePathsInDiamond()
    {
        var root = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var leaf = Guid.NewGuid();
        var query = new FakeCompositionReadQuery(
        [
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(left, [root, left], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(right, [root, right], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(leaf, [root, left, leaf], [root, left], PdmObjectType.Part, Guid.NewGuid()),
            Occurrence(leaf, [root, right, leaf], [root, right], PdmObjectType.Part, Guid.NewGuid())
        ]);
        var handler = new GetCompositionQueryHandler(query);

        var result = await handler.Handle(new GetCompositionQuery(root), CancellationToken.None);

        Assert.NotNull(result);
        var tree = result!;
        Assert.Equal(root, tree.RootObjectId);
        var leafNodes = tree.Nodes.Where(node => node.ObjectId == leaf).ToArray();
        Assert.Equal(2, leafNodes.Length);
        Assert.NotEqual(leafNodes[0].ObjectPath, leafNodes[1].ObjectPath);
        Assert.Equal(new[] { root, left, leaf }, leafNodes[0].ObjectPath);
        Assert.Equal(new[] { root, right, leaf }, leafNodes[1].ObjectPath);
        Assert.All(leafNodes, node => Assert.Equal("Part", node.Type));
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToSingleRead()
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var query = new FakeCompositionReadQuery([Occurrence(id, [id], null, PdmObjectType.Part, Guid.NewGuid())]);

        var result = await new GetCompositionQueryHandler(query).Handle(new GetCompositionQuery(id), cts.Token);

        Assert.NotNull(result);
        Assert.Equal(1, query.ReadCount);
        Assert.Equal(cts.Token, query.ReceivedToken);
    }

    [Fact]
    public async Task Handle_ReturnsNullWhenRootIsMissing()
    {
        var query = new FakeCompositionReadQuery([]);

        var result = await new GetCompositionQueryHandler(query)
            .Handle(new GetCompositionQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_RetainsNodeWithoutCurrentVersionWithDiagnostic()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var query = new FakeCompositionReadQuery(
        [
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(child, [root, child], [root], PdmObjectType.Part, null, name: "Missing version")
        ]);

        var result = await new GetCompositionQueryHandler(query).Handle(new GetCompositionQuery(root), CancellationToken.None);

        Assert.NotNull(result);
        var tree = result!;
        var node = Assert.Single(tree.Nodes, node => node.ObjectId == child);
        Assert.Equal("NoCurrentVersion", node.ErrorCode);
        Assert.NotNull(node.Error);
        Assert.Null(node.VersionId);
        Assert.Equal("Missing version", node.Name);
    }

    [Fact]
    public async Task Handle_MarksCycleWithTheOccurrencePath()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var cyclePath = new[] { root, child, root };
        var query = new FakeCompositionReadQuery(
        [
            Occurrence(root, [root], null, PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(child, [root, child], [root], PdmObjectType.Assembly, Guid.NewGuid()),
            Occurrence(root, cyclePath, [root, child], PdmObjectType.Assembly, Guid.NewGuid(), isCycle: true)
        ]);

        var result = await new GetCompositionQueryHandler(query).Handle(new GetCompositionQuery(root), CancellationToken.None);

        Assert.NotNull(result);
        var tree = result!;
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

    private sealed class FakeCompositionReadQuery(IReadOnlyList<CompositionOccurrence> result) : ICompositionReadQuery
    {
        public int ReadCount { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyList<CompositionOccurrence>> ReadAsync(Guid rootObjectId, CancellationToken cancellationToken)
        {
            ReadCount++;
            ReceivedToken = cancellationToken;
            return Task.FromResult(result);
        }
    }
}
