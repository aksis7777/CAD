using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Composition.Features.ReplaceComposition;
using MiniPdm.Storage.Abstractions.Versions;
using Xunit;

namespace MiniPdm.Modules.Tests;

public sealed class VersionMutationHandlerTests
{
    [Fact]
    public async Task ReplaceComposition_PassesDistinctChildIdsAndDomainPlanToPersistence()
    {
        var childId = Guid.NewGuid();
        var snapshot = CreateAssemblySnapshot(childId);
        var persistence = new CapturingPersistence(snapshot);
        var handler = new ReplaceCompositionCommandHandler(persistence);
        var cancellation = new CancellationTokenSource();
        var command = new ReplaceCompositionCommand(snapshot.Object.Id, 1,
            [new CompositionItem(childId, 2), new CompositionItem(childId, 3)],
            snapshot.Object.ConcurrencyToken);

        var result = await handler.Handle(command, cancellation.Token);

        Assert.Equal(cancellation.Token, persistence.CancellationToken);
        Assert.Equal(new[] { childId }, persistence.Request!.ReferencedChildIds);
        Assert.Equal(VersionMutationStatus.Succeeded, persistence.PreparedPlan!.Status);
        Assert.Equal(new[] { 5 }, persistence.PreparedPlan.Version!.Components.Select(x => x.Quantity));
        Assert.Single(persistence.PreparedPlan.Warnings);
        Assert.Equal(VersionMutationStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task HandlerPropagatesCancellationWithoutCallingWritePort()
    {
        var snapshot = CreateAssemblySnapshot();
        var persistence = new CapturingPersistence(snapshot);
        var handler = new ReplaceCompositionCommandHandler(persistence);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(
            new ReplaceCompositionCommand(snapshot.Object.Id, 1, [], snapshot.Object.ConcurrencyToken),
            cancellation.Token));

        Assert.Null(persistence.Request);
    }

    private static VersionMutationSnapshot CreateAssemblySnapshot(Guid childId = default)
    {
        var obj = new PdmObject { Type = PdmObjectType.Assembly, Designation = "АБВГ.123456.001" };
        var version = new ObjectVersion { ObjectId = obj.Id, Object = obj, Version = 1, State = VersionState.InWork };
        obj.CurrentVersionId = version.Id;
        obj.CurrentVersion = version;
        obj.Versions.Add(version);
        return new VersionMutationSnapshot(obj, version, [], new HashSet<Guid> { childId });
    }

    private sealed class CapturingPersistence(VersionMutationSnapshot snapshot) : IVersionWritePersistence
    {
        public VersionWriteRequest? Request { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public VersionMutationPlan? PreparedPlan { get; private set; }

        public Task<VersionMutationResult> ExecuteAsync(VersionWriteRequest request,
            Func<VersionMutationSnapshot, VersionMutationPlan> prepare, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Request = request;
            CancellationToken = ct;
            PreparedPlan = prepare(snapshot);
            var version = PreparedPlan.NewVersion ?? PreparedPlan.Version;
            return Task.FromResult(new VersionMutationResult(PreparedPlan.Status, request.ObjectId,
                version?.Id, version?.Version, version?.State, PreparedPlan.DesiredCurrentVersionId,
                snapshot.Object.ConcurrencyToken, PreparedPlan.Error, PreparedPlan.Warnings));
        }
    }
}
