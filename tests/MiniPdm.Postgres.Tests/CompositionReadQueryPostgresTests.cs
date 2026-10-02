using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Storage;
using MiniPdm.Storage.Concurrency;
using MiniPdm.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;
using Npgsql;
using Xunit;

namespace MiniPdm.Postgres.Tests;

public sealed class CompositionReadQueryPostgresTests
{
    private const string ConnectionVariable = "PDM_TEST_POSTGRES_CONNECTION";

    [Fact]
    public async Task Missing_root_and_empty_root_have_expected_occurrences_in_one_statement()
    {
        await using var fixture = await Fixture.OpenAsync();
        var query = new CompositionReadService(fixture.Context);

        var missingRootId = Guid.NewGuid();
        fixture.Commands.Reset();
        var missing = await query.ReadAsync(missingRootId, CancellationToken.None);
        Assert.Empty(missing);
        Assert.Equal(1, fixture.Commands.Count);

        var root = NewObject(PdmObjectType.Assembly, "EMPTYROOT");
        var version = NewVersion(root, 1, VersionState.InWork, "Empty assembly");
        await fixture.SaveObjectsAsync([root]);
        await fixture.SaveVersionsAsync([version]);
        await fixture.SetCurrentVersionsAsync((root, version));

        fixture.Commands.Reset();
        var rows = await query.ReadAsync(root.Id, CancellationToken.None);
        Assert.Equal(1, fixture.Commands.Count);
        var occurrence = Assert.Single(rows);
        Assert.Equal(root.Id, occurrence.ObjectId);
        Assert.Equal(new[] { root.Id }, occurrence.ObjectPath);
        Assert.Null(occurrence.ParentPath);
        Assert.Equal(version.Id, occurrence.VersionId);
    }

    [Fact]
    public async Task Active_graph_projection_excludes_cancelled_pointers_and_nonassembly_parents()
    {
        await using var fixture = await Fixture.OpenAsync();
        var activeParent = NewObject(PdmObjectType.Assembly, "GRAPHACTIVE");
        var cancelledParent = NewObject(PdmObjectType.Assembly, "GRAPHCANCELLED");
        var partParent = NewObject(PdmObjectType.Part, "GRAPHPART");
        var child = NewObject(PdmObjectType.Part, "GRAPHCHILD");
        var activeVersion = NewVersion(activeParent, 1, VersionState.Approved, "Active");
        var cancelledVersion = NewVersion(cancelledParent, 1, VersionState.Cancelled, "Cancelled");
        var partVersion = NewVersion(partParent, 1, VersionState.Approved, "Part");
        await fixture.SaveObjectsAsync([activeParent, cancelledParent, partParent, child]);
        await fixture.SaveVersionsAsync([activeVersion, cancelledVersion, partVersion]);
        await fixture.SetCurrentVersionsAsync((activeParent, activeVersion), (cancelledParent, cancelledVersion), (partParent, partVersion));
        await fixture.SaveLinksAsync(
            Link(activeVersion.Id, child.Id, 1),
            Link(cancelledVersion.Id, child.Id, 1),
            Link(partVersion.Id, child.Id, 1));

        fixture.Commands.Reset();
        var edges = await ActiveCompositionGraphQuery.LoadAsync(fixture.Context, CancellationToken.None);

        Assert.Equal(1, fixture.Commands.Count);
        var fixtureParentIds = new HashSet<Guid> { activeParent.Id, cancelledParent.Id, partParent.Id };
        Assert.Equal(new[] { new MiniPdm.Domain.Composition.CompositionGraphEdge(activeParent.Id, child.Id) },
            edges.Where(edge => fixtureParentIds.Contains(edge.ParentId)));
    }

    [Fact]
    public async Task Diamond_returns_each_path_with_local_quantities_and_one_statement()
    {
        await using var fixture = await Fixture.OpenAsync();
        var root = NewObject(PdmObjectType.Assembly, "DIAMOND");
        var left = NewObject(PdmObjectType.Assembly, "LEFT");
        var right = NewObject(PdmObjectType.Assembly, "RIGHT");
        var leaf = NewObject(PdmObjectType.Part, "SHARED");
        var rootVersion = NewVersion(root, 1, VersionState.Approved, "Root", "Steel", 20m);
        var leftVersion = NewVersion(left, 1, VersionState.Approved, "Left");
        var rightVersion = NewVersion(right, 1, VersionState.Approved, "Right");
        var leafVersion = NewVersion(leaf, 1, VersionState.Approved, "Shared part", "Aluminium", 1.25m);

        await fixture.SaveObjectsAsync([root, left, right, leaf]);
        await fixture.SaveVersionsAsync([rootVersion, leftVersion, rightVersion, leafVersion]);
        await fixture.SetCurrentVersionsAsync((root, rootVersion), (left, leftVersion), (right, rightVersion), (leaf, leafVersion));
        await fixture.SaveLinksAsync(
            Link(rootVersion.Id, left.Id, 2), Link(rootVersion.Id, right.Id, 3),
            Link(leftVersion.Id, leaf.Id, 4), Link(rightVersion.Id, leaf.Id, 5));

        fixture.Commands.Reset();
        var rows = await new CompositionReadService(fixture.Context).ReadAsync(root.Id, CancellationToken.None);

        Assert.Equal(1, fixture.Commands.Count);
        Assert.Equal(5, rows.Count);
        var rootRow = Assert.Single(rows, x => x.ObjectPath.Length == 1);
        Assert.Equal(root.Id, rootRow.ObjectId);
        var leftRow = Assert.Single(rows, x => x.ObjectId == left.Id);
        var rightRow = Assert.Single(rows, x => x.ObjectId == right.Id);
        Assert.Equal(new[] { root.Id }, leftRow.ParentPath);
        Assert.Equal(new[] { root.Id }, rightRow.ParentPath);
        Assert.Equal(2, leftRow.LocalQuantity);
        Assert.Equal(3, rightRow.LocalQuantity);

        var leaves = rows.Where(x => x.ObjectId == leaf.Id).ToArray();
        Assert.Equal(2, leaves.Length);
        var throughLeft = Assert.Single(leaves, x => x.ParentPath!.SequenceEqual(new[] { root.Id, left.Id }));
        var throughRight = Assert.Single(leaves, x => x.ParentPath!.SequenceEqual(new[] { root.Id, right.Id }));
        Assert.Equal(new[] { root.Id, left.Id, leaf.Id }, throughLeft.ObjectPath);
        Assert.Equal(new[] { root.Id, right.Id, leaf.Id }, throughRight.ObjectPath);
        Assert.Equal(4, throughLeft.LocalQuantity);
        Assert.Equal(5, throughRight.LocalQuantity);
        Assert.Equal("Shared part", throughLeft.Name);
        Assert.Equal("Aluminium", throughLeft.Material);
        Assert.Equal(1.25m, throughLeft.UnitMassKg);

        fixture.Commands.Reset();
        var tree = await new CompositionReadService(fixture.Context).GetCompositionAsync(root.Id, CancellationToken.None);
        Assert.Equal(1, fixture.Commands.Count);
        var sharedOccurrences = tree!.Nodes.Where(x => x.ObjectId == leaf.Id).ToArray();
        Assert.Equal(2, sharedOccurrences.Length);
        Assert.All(sharedOccurrences, node => Assert.Equal("Part", node.Type));
        Assert.Contains(sharedOccurrences, node => node.ObjectPath.SequenceEqual(new[] { root.Id, left.Id, leaf.Id }));
        Assert.Contains(sharedOccurrences, node => node.ObjectPath.SequenceEqual(new[] { root.Id, right.Id, leaf.Id }));
    }

    [Fact]
    public async Task Query_reloads_new_child_current_version_and_hides_it_after_cancellation()
    {
        await using var fixture = await Fixture.OpenAsync();
        var root = NewObject(PdmObjectType.Assembly, "RELOADROOT");
        var child = NewObject(PdmObjectType.Part, "CANCELCHILD");
        var rootVersion = NewVersion(root, 1, VersionState.Approved, "Parent before", "Parent material", 20m);
        var childVersion = NewVersion(child, 1, VersionState.Approved, "Child approved", "Approved alloy", 7m);
        await fixture.SaveObjectsAsync([root, child]);
        await fixture.SaveVersionsAsync([rootVersion, childVersion]);
        await fixture.SetCurrentVersionsAsync((root, rootVersion), (child, childVersion));
        await fixture.SaveLinksAsync(Link(rootVersion.Id, child.Id, 6));

        var query = new CompositionReadService(fixture.Context);
        var initial = await query.ReadAsync(root.Id, CancellationToken.None);
        Assert.Equal("Parent before", Assert.Single(initial, x => x.ObjectId == root.Id).Name);
        Assert.Equal("Child approved", Assert.Single(initial, x => x.ObjectId == child.Id).Name);

        var childCurrent = NewVersion(child, 2, VersionState.InWork, "Child in work", "New alloy", 8.5m);
        await fixture.SaveVersionsAsync(childCurrent);
        await fixture.SetCurrentVersionsAsync((child, childCurrent));

        fixture.Commands.Reset();
        var reloaded = await query.ReadAsync(root.Id, CancellationToken.None);
        Assert.Equal(1, fixture.Commands.Count);
        var rootRow = Assert.Single(reloaded, x => x.ObjectId == root.Id);
        Assert.Equal(rootVersion.Id, rootRow.VersionId);
        Assert.Equal(VersionState.Approved, rootRow.State);
        Assert.Equal("Parent before", rootRow.Name);
        Assert.Equal("Parent material", rootRow.Material);
        Assert.Null(rootRow.UnitMassKg);
        var childRow = Assert.Single(reloaded, x => x.ObjectId == child.Id);
        Assert.Equal(6, childRow.LocalQuantity);
        Assert.Equal(childCurrent.Id, childRow.VersionId);
        Assert.Equal(2, childRow.VersionNumber);
        Assert.Equal(VersionState.InWork, childRow.State);
        Assert.Equal("Child in work", childRow.Name);
        Assert.Equal("New alloy", childRow.Material);
        Assert.Equal(8.5m, childRow.UnitMassKg);

        childCurrent.State = VersionState.Cancelled;
        await fixture.Context.SaveChangesAsync();

        fixture.Commands.Reset();
        var afterCancellation = await query.ReadAsync(root.Id, CancellationToken.None);
        Assert.Equal(1, fixture.Commands.Count);
        var cancelledChild = Assert.Single(afterCancellation, x => x.ObjectId == child.Id);
        Assert.Equal(6, cancelledChild.LocalQuantity);
        Assert.Null(cancelledChild.VersionId);
        Assert.Null(cancelledChild.VersionNumber);
        Assert.Null(cancelledChild.State);
        Assert.Null(cancelledChild.Name);
        Assert.Null(cancelledChild.Material);
        Assert.Null(cancelledChild.UnitMassKg);
    }

    [Fact]
    public async Task Root_without_current_version_is_retained_as_leaf()
    {
        await using var fixture = await Fixture.OpenAsync();
        var root = NewObject(PdmObjectType.Assembly, "NOVERSION");
        await fixture.SaveObjectsAsync([root]);

        fixture.Commands.Reset();
        var rows = await new CompositionReadService(fixture.Context).ReadAsync(root.Id, CancellationToken.None);

        Assert.Equal(1, fixture.Commands.Count);
        var row = Assert.Single(rows);
        Assert.Equal(root.Id, row.ObjectId);
        Assert.Equal(new[] { root.Id }, row.ObjectPath);
        Assert.Null(row.VersionId);
        Assert.Null(row.Name);
    }

    [Fact]
    public async Task Cycle_is_emitted_once_as_a_leaf_and_query_terminates_in_one_statement()
    {
        await using var fixture = await Fixture.OpenAsync();
        var a = NewObject(PdmObjectType.Assembly, "CYCLEA");
        var b = NewObject(PdmObjectType.Assembly, "CYCLEB");
        var av = NewVersion(a, 1, VersionState.Approved, "A");
        var bv = NewVersion(b, 1, VersionState.Approved, "B");
        await fixture.SaveObjectsAsync([a, b]);
        await fixture.SaveVersionsAsync([av, bv]);
        await fixture.SetCurrentVersionsAsync((a, av), (b, bv));
        await fixture.SaveLinksAsync(Link(av.Id, b.Id, 2), Link(bv.Id, a.Id, 3));

        fixture.Commands.Reset();
        var rows = await new CompositionReadService(fixture.Context).ReadAsync(a.Id, CancellationToken.None);

        Assert.Equal(1, fixture.Commands.Count);
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { a.Id, b.Id, a.Id }, rows.Single(x => x.IsCycle).ObjectPath);
        Assert.Equal(1, rows.Count(x => x.IsCycle));
        Assert.Equal(3, rows.Single(x => x.IsCycle).LocalQuantity);
        Assert.All(rows.Where(x => !x.IsCycle), x => Assert.False(x.IsCycle));
    }

    [Fact]
    public async Task Part_root_is_a_single_leaf_occurrence()
    {
        await using var fixture = await Fixture.OpenAsync();
        var root = NewObject(PdmObjectType.Part, "PARTROOT");
        var version = NewVersion(root, 1, VersionState.Approved, "Root part", "Titanium", 0.75m);
        await fixture.SaveObjectsAsync([root]);
        await fixture.SaveVersionsAsync([version]);
        await fixture.SetCurrentVersionsAsync((root, version));

        fixture.Commands.Reset();
        var rows = await new CompositionReadService(fixture.Context).ReadAsync(root.Id, CancellationToken.None);

        Assert.Equal(1, fixture.Commands.Count);
        var row = Assert.Single(rows);
        Assert.False(row.IsCycle);
        Assert.Equal(root.Id, row.ObjectId);
        Assert.Equal("Root part", row.Name);
        Assert.Equal("Titanium", row.Material);
        Assert.Equal(0.75m, row.UnitMassKg);
    }

    private static PdmObject NewObject(PdmObjectType type, string designation) => new()
    {
        Type = type,
        Designation = designation,
        NormalizedName = null,
        StandardName = null
    };

    private static ObjectVersion NewVersion(PdmObject owner, int number, VersionState state, string name, string? material = null, decimal? mass = null) => new()
    {
        ObjectId = owner.Id,
        Version = number,
        State = state,
        Name = name,
        Material = material,
        Mass = mass
    };

    private static BomLink Link(Guid parentVersionId, Guid childObjectId, int quantity) => new()
    {
        ParentVersionId = parentVersionId,
        ChildObjectId = childObjectId,
        Quantity = quantity
    };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction _transaction;

        private Fixture(NpgsqlConnection connection, NpgsqlTransaction transaction, PdmDbContext context, CountingCommandInterceptor commands)
        {
            _connection = connection;
            _transaction = transaction;
            Context = context;
            Commands = commands;
        }

        public PdmDbContext Context { get; }
        public CountingCommandInterceptor Commands { get; }

        public static async Task<Fixture> OpenAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Set {ConnectionVariable} to a dedicated, already-migrated PostgreSQL test database before running these integration tests.");

            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            try
            {
                var transaction = await connection.BeginTransactionAsync();
                var commands = new CountingCommandInterceptor();
                var options = new DbContextOptionsBuilder<PdmDbContext>()
                    .UseNpgsql(connection)
                    .AddInterceptors(commands)
                    .Options;
                var context = new PdmDbContext(options);
                await context.Database.UseTransactionAsync(transaction);
                return new Fixture(connection, transaction, context, commands);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public async Task SaveObjectsAsync(params PdmObject[] objects)
        {
            Context.Objects.AddRange(objects);
            await Context.SaveChangesAsync();
        }

        public async Task SaveVersionsAsync(params ObjectVersion[] versions)
        {
            Context.Versions.AddRange(versions);
            await Context.SaveChangesAsync();
        }

        public async Task SetCurrentVersionsAsync(params (PdmObject Object, ObjectVersion Version)[] pointers)
        {
            foreach (var (obj, version) in pointers) obj.CurrentVersionId = version.Id;
            await Context.SaveChangesAsync();
        }

        public async Task SaveLinksAsync(params BomLink[] links)
        {
            Context.BomLinks.AddRange(links);
            await Context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }
    }
}
