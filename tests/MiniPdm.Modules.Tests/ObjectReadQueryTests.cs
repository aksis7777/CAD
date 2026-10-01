using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Objects.Features.GetObject;
using MiniPdm.Modules.Objects.Features.SearchObjects;
using MiniPdm.Storage.Abstractions.Objects;
using Xunit;

namespace MiniPdm.Modules.Tests;

public sealed class ObjectReadQueryTests
{
    [Fact]
    public async Task Card_defaults_to_current_version_and_uses_standard_name_for_standard_part()
    {
        var current = Version(1, VersionState.Approved, "Version label");
        var storage = new FakeObjectReadQuery(Card(PdmObjectType.StandardPart, "M8 screw", current.Id, [current]));

        var result = await new GetObjectQueryHandler(storage).Handle(new GetObjectQuery(Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal("M8 screw", result!.Name);
        Assert.Equal(current.Id, result.CurrentVersionId);
        Assert.True(result.SelectedVersion!.IsCurrent);
        Assert.Equal("Approved", result.SelectedVersion.State);
        Assert.Equal(current.Id, storage.RequestedVersion?.Id);
    }

    [Fact]
    public async Task Card_can_display_cancelled_historical_version_without_marking_it_current()
    {
        var current = Version(1, VersionState.Approved, "Current");
        var historical = Version(2, VersionState.Cancelled, "Cancelled");
        var storage = new FakeObjectReadQuery(Card(PdmObjectType.Part, null, current.Id, [historical, current]), explicitVersion: historical);

        var result = await new GetObjectQueryHandler(storage).Handle(new GetObjectQuery(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.Equal(2, result!.SelectedVersion!.Version);
        Assert.Equal("Cancelled", result.SelectedVersion.State);
        Assert.False(result.SelectedVersion.IsCurrent);
        Assert.Equal(current.Id, result.CurrentVersionId);
        Assert.Contains(result.Versions, v => v.Version == 2 && v.State == "Cancelled");
    }

    [Fact]
    public async Task Card_keeps_existing_object_without_current_version_and_missing_object_returns_null()
    {
        var storage = new FakeObjectReadQuery(Card(PdmObjectType.Assembly, null, null, []));
        var handler = new GetObjectQueryHandler(storage);

        var noCurrent = await handler.Handle(new GetObjectQuery(Guid.NewGuid(), null), CancellationToken.None);
        var missing = await new GetObjectQueryHandler(new FakeObjectReadQuery(null))
            .Handle(new GetObjectQuery(Guid.NewGuid(), null), CancellationToken.None);

        Assert.Null(noCurrent!.CurrentVersionId);
        Assert.Null(noCurrent.SelectedVersion);
        Assert.Equal("NoCurrentVersion", noCurrent.ErrorCode);
        Assert.Null(missing);
    }

    [Fact]
    public async Task Search_maps_storage_rows_and_passes_cancellation_token()
    {
        var id = Guid.NewGuid();
        var storage = new FakeObjectReadQuery(null,
            new ObjectSearchPage([new ObjectSearchRow(id, PdmObjectType.Part, "АБВГ.301245.001", "Part", Guid.NewGuid(), 4,
                VersionState.InWork, 2.25m, Guid.NewGuid(), false)], 3, 10, true));
        using var source = new CancellationTokenSource();

        ObjectSearchPageDto page = await new SearchObjectsQueryHandler(storage).Handle(new SearchObjectsQuery("Part", 3, 10), source.Token);

        Assert.Equal(3, page.Offset);
        Assert.True(page.HasMore);
        Assert.Equal("Part", Assert.Single(page.Items).Type);
        Assert.Equal("InWork", page.Items[0].State);
        Assert.Equal(source.Token, storage.ReceivedToken);
    }

    private static ObjectVersionReadRow Version(int number, VersionState state, string name) =>
        new(Guid.NewGuid(), number, state, name, "Steel", 1m, null);

    private static ObjectCardReadRow Card(PdmObjectType type, string? standardName, Guid? current, IReadOnlyList<ObjectVersionReadRow> versions) =>
        new(Guid.NewGuid(), type, type == PdmObjectType.StandardPart ? null : "АБВГ.301245.001", standardName, current, Guid.NewGuid(),
            versions.Select(v => new ObjectVersionSummaryReadRow(v.Id, v.Version, v.State)).ToArray(),
            versions.FirstOrDefault(v => v.Id == current) ?? versions.FirstOrDefault());

    private sealed class FakeObjectReadQuery(ObjectCardReadRow? card, ObjectSearchPage? page = null, ObjectVersionReadRow? explicitVersion = null) : IObjectReadQuery
    {
        public CancellationToken ReceivedToken { get; private set; }
        public ObjectVersionReadRow? RequestedVersion { get; private set; }

        public Task<ObjectSearchPage> SearchAsync(string search, int offset, int limit, CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            return Task.FromResult(page ?? new ObjectSearchPage([], offset, limit, false));
        }

        public Task<ObjectCardReadRow?> GetAsync(Guid objectId, int? versionNumber, CancellationToken cancellationToken)
        {
            var result = card is null ? null : versionNumber.HasValue
                ? card with { SelectedVersion = explicitVersion }
                : card;
            RequestedVersion = result?.SelectedVersion;
            return Task.FromResult(result);
        }
    }
}
