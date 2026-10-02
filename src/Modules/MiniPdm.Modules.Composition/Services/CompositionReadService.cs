using Microsoft.EntityFrameworkCore;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Composition.DtoModels;
using MiniPdm.Storage;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using Npgsql;

namespace MiniPdm.Modules.Composition.Services;

/// <summary>Reads a composition tree through one PostgreSQL recursive CTE.</summary>
public sealed class CompositionReadService(PdmDbContext context)
{
    private const string Sql = """
        WITH RECURSIVE composition AS (
            SELECT
                o."Id" AS "ObjectId",
                ARRAY[o."Id"]::uuid[] AS "ObjectPath",
                NULL::uuid[] AS "ParentPath",
                1::integer AS "LocalQuantity",
                o."Type" AS "TypeValue",
                o."Designation" AS "Designation",
                CASE WHEN o."Type" = 3 THEN o."StandardName" ELSE v."Name" END AS "Name",
                v."Material" AS "Material",
                v."Id" AS "VersionId",
                v."Version" AS "VersionNumber",
                v."State" AS "StateValue",
                CASE WHEN o."Type" = 1 THEN NULL::numeric ELSE v."Mass" END AS "UnitMassKg",
                false AS "IsCycle"
            FROM pdm_objects AS o
            LEFT JOIN object_versions AS v
                ON v."Id" = o."CurrentVersionId"
                AND v."ObjectId" = o."Id"
                AND v."State" <> 3
            WHERE o."Id" = @rootObjectId

            UNION ALL

            SELECT
                child."Id" AS "ObjectId",
                parent."ObjectPath" || child."Id" AS "ObjectPath",
                parent."ObjectPath" AS "ParentPath",
                link."Quantity" AS "LocalQuantity",
                child."Type" AS "TypeValue",
                child."Designation" AS "Designation",
                CASE WHEN child."Type" = 3 THEN child."StandardName" ELSE childVersion."Name" END AS "Name",
                childVersion."Material" AS "Material",
                childVersion."Id" AS "VersionId",
                childVersion."Version" AS "VersionNumber",
                childVersion."State" AS "StateValue",
                CASE WHEN child."Type" = 1 THEN NULL::numeric ELSE childVersion."Mass" END AS "UnitMassKg",
                child."Id" = ANY(parent."ObjectPath") AS "IsCycle"
            FROM composition AS parent
            JOIN bom_links AS link ON link."ParentVersionId" = parent."VersionId"
            JOIN pdm_objects AS child ON child."Id" = link."ChildObjectId"
            LEFT JOIN object_versions AS childVersion
                ON childVersion."Id" = child."CurrentVersionId"
                AND childVersion."ObjectId" = child."Id"
                AND childVersion."State" <> 3
            WHERE parent."TypeValue" = 1
                AND parent."VersionId" IS NOT NULL
                AND NOT parent."IsCycle"
        )
        SELECT * FROM composition
        ORDER BY cardinality("ObjectPath"), "ObjectPath"
        """;

    public async Task<CompositionTreeDto?> GetCompositionAsync(Guid rootObjectId, CancellationToken cancellationToken)
    {
        var occurrences = await ReadAsync(rootObjectId, cancellationToken);
        return occurrences.Count == 0 ? null : MapTreeDto(rootObjectId, occurrences);
    }

    public static CompositionTreeDto MapTreeDto(Guid rootObjectId, IReadOnlyList<CompositionOccurrence> occurrences)
    {
        var nodes = occurrences.Select(occurrence =>
        {
            var (errorCode, error) = GetDiagnostic(occurrence);
            return new CompositionNodeDto(occurrence.ObjectId, occurrence.ObjectPath, occurrence.ParentPath,
                occurrence.LocalQuantity, ToTypeName(occurrence.Type), occurrence.Designation, occurrence.Name,
                occurrence.Material, occurrence.VersionId, occurrence.VersionNumber,
                occurrence.State is VersionState state ? ToStateName(state) : null,
                occurrence.UnitMassKg, errorCode, error);
        }).ToArray();
        return new CompositionTreeDto(rootObjectId, nodes);
    }

    private static (string? ErrorCode, string? Error) GetDiagnostic(CompositionOccurrence occurrence)
    {
        if (occurrence.IsCycle)
            return ("Cycle", $"Composition cycle detected along path {string.Join(" → ", occurrence.ObjectPath)}.");
        return occurrence.VersionId is null
            ? ("NoCurrentVersion", "The object has no current non-cancelled version.")
            : (null, null);
    }

    private static string ToTypeName(PdmObjectType type) => type switch
    {
        PdmObjectType.Assembly => "Assembly",
        PdmObjectType.Part => "Part",
        PdmObjectType.StandardPart => "StandardPart",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown PDM object type.")
    };

    private static string ToStateName(VersionState state) => state switch
    {
        VersionState.InWork => "InWork",
        VersionState.Approved => "Approved",
        VersionState.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown version state.")
    };

    public async Task<IReadOnlyList<CompositionOccurrence>> ReadAsync(Guid rootObjectId, CancellationToken cancellationToken)
    {
        var rows = await context.Database.SqlQueryRaw<CompositionRow>(Sql, new NpgsqlParameter("rootObjectId", rootObjectId))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new CompositionOccurrence(
            row.ObjectId,
            row.ObjectPath,
            row.ParentPath,
            row.LocalQuantity,
            (PdmObjectType)row.TypeValue,
            row.Designation,
            row.Name,
            row.Material,
            row.VersionId,
            row.VersionNumber,
            row.StateValue is int state ? (VersionState)state : null,
            row.UnitMassKg,
            row.IsCycle)).ToArray();
    }

    // EF Core maps this private result type directly from the SQL projection; enum values are converted client-side.
    private sealed class CompositionRow
    {
        public Guid ObjectId { get; set; }
        public Guid[] ObjectPath { get; set; } = [];
        public Guid[]? ParentPath { get; set; }
        public int LocalQuantity { get; set; }
        public int TypeValue { get; set; }
        public string? Designation { get; set; }
        public string? Name { get; set; }
        public string? Material { get; set; }
        public Guid? VersionId { get; set; }
        public int? VersionNumber { get; set; }
        public int? StateValue { get; set; }
        public decimal? UnitMassKg { get; set; }
        public bool IsCycle { get; set; }
    }
}
