using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Domain.Calculations;

/// <summary>
///     Вхождение объекта в развёрнутом дереве с данными для расчёта массы и спецификации.
/// </summary>
/// <param name="ObjectId">
///     Идентификатор объекта в этом вхождении.
/// </param>
/// <param name="ObjectPath">
///     Идентификаторы объектов от корня до этого вхождения.
/// </param>
/// <param name="ParentPath">
///     Путь родительского вхождения; для корня равен null.
/// </param>
/// <param name="LocalQuantity">
///     Положительное целое количество экземпляров на связи с родительским вхождением.
/// </param>
/// <param name="Type">
///     Тип объекта PDM.
/// </param>
/// <param name="Designation">
///     Обозначение детали или сборки, если оно применимо.
/// </param>
/// <param name="Name">
///     Отображаемое наименование объекта, если оно задано.
/// </param>
/// <param name="Material">
///     Материал текущей версии, если он применим.
/// </param>
/// <param name="VersionId">
///     Идентификатор текущей версии; null, если действующей версии нет.
/// </param>
/// <param name="VersionNumber">
///     Номер текущей версии, если он задан.
/// </param>
/// <param name="State">
///     Состояние текущей версии, если она задана.
/// </param>
/// <param name="UnitMassKg">
///     Масса одного изделия в килограммах, если она известна.
/// </param>
/// <param name="IsCycle">
///     Замыкает ли это вхождение цикл состава.
/// </param>
public sealed record CompositionCalculationInput(
    Guid ObjectId,
    IReadOnlyList<Guid> ObjectPath,
    IReadOnlyList<Guid>? ParentPath,
    int LocalQuantity,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    VersionState? State,
    decimal? UnitMassKg,
    bool IsCycle);

/// <summary>
///     Рассчитанная строка спецификации: количество указано в штуках, масса — в килограммах.
/// </summary>
/// <param name="ObjectId">
///     Идентификатор объекта спецификации.
/// </param>
/// <param name="Type">
///     Тип объекта PDM.
/// </param>
/// <param name="Designation">
///     Обозначение детали или сборки, если оно применимо.
/// </param>
/// <param name="Name">
///     Отображаемое наименование объекта, если оно задано.
/// </param>
/// <param name="Material">
///     Материал текущей версии, если он применим.
/// </param>
/// <param name="VersionId">
///     Идентификатор текущей версии; null, если действующей версии нет.
/// </param>
/// <param name="VersionNumber">
///     Номер текущей версии, если он задан.
/// </param>
/// <param name="Quantity">
///     Количество целых изделий, суммированное по всем путям; null означает, что хотя бы одно слагаемое неизвестно.
/// </param>
/// <param name="UnitMassKg">
///     Масса одного изделия в килограммах, если она известна.
/// </param>
/// <param name="TotalMassKg">
///     Количество, умноженное на массу одного изделия; null, если одно из значений неизвестно.
/// </param>
public sealed record CalculatedSpecificationItem(
    Guid ObjectId,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    decimal? Quantity,
    decimal? UnitMassKg,
    decimal? TotalMassKg);

/// <summary>
///     Описывает отсутствующее или неверное значение, из-за которого расчёт состава неполон.
/// </summary>
/// <param name="Code">
///     Стабильный код диагностики, например <c>MissingMass</c> или <c>Cycle</c>.
/// </param>
/// <param name="ObjectId">
///     Идентификатор объекта, связанного с проблемой.
/// </param>
/// <param name="ObjectPath">
///     Путь от корневого объекта до проблемного вхождения.
/// </param>
/// <param name="Message">
///     Понятное пользователю описание проблемы.
/// </param>
public sealed record CalculationDiagnostic(string Code, Guid ObjectId, IReadOnlyList<Guid> ObjectPath, string Message);

/// <summary>
///     Результат расчёта спецификации и массы. При наличии диагностики общая масса считается неизвестной.
/// </summary>
/// <param name="RootObjectId">
///     Идентификатор корневого объекта расчёта.
/// </param>
/// <param name="TotalMassKg">
///     Рассчитанная общая масса; null, если возникла хотя бы одна диагностика.
/// </param>
/// <param name="IsComplete">
///     Равно true только при отсутствии диагностик расчёта.
/// </param>
/// <param name="Items">
///     Плоские строки спецификации, сгруппированные по объекту.
/// </param>
/// <param name="Diagnostics">
///     Проблемы, обнаруженные при расчёте дерева объектов.
/// </param>
public sealed record CompositionCalculationResult(
    Guid RootObjectId,
    decimal? TotalMassKg,
    bool IsComplete,
    IReadOnlyList<CalculatedSpecificationItem> Items,
    IReadOnlyList<CalculationDiagnostic> Diagnostics);

/// <summary>
///     Рассчитывает количества в составе, массы деталей и плоскую спецификацию без обращения к хранилищу.
/// </summary>
public static class CompositionCalculator
{
    /// <summary>
    ///     Рассчитывает массу корневого объекта и плоскую спецификацию по развёрнутым вхождениям дерева.
    /// </summary>
    /// <param name="rootObjectId">
    ///     Идентификатор объекта, состав которого рассчитывается.
    /// </param>
    /// <param name="occurrences">
    ///     Вхождения из развёрнутого дерева состава.
    /// </param>
    /// <returns>
    ///     Рассчитанная спецификация, общая масса и найденные диагностики.
    /// </returns>
    public static CompositionCalculationResult Calculate(Guid rootObjectId, IEnumerable<CompositionCalculationInput> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);

        var diagnostics = new List<CalculationDiagnostic>();
        var quantityByPath = new Dictionary<string, decimal?>(StringComparer.Ordinal);
        var groups = new Dictionary<Guid, SpecificationAccumulator>();
        var ordered = occurrences.OrderBy(x => x.ObjectPath.Count)
            .ThenBy(x => PathKey(x.ObjectPath), StringComparer.Ordinal).ToArray();

        foreach (var occurrence in ordered)
        {
            var path = occurrence.ObjectPath;
            var pathKey = PathKey(path);
            if (occurrence.IsCycle)
            {
                diagnostics.Add(Diagnostic("Cycle", occurrence,
                    "The composition path repeats an object and cannot be included in the calculation."));
                continue;
            }

            decimal? quantity;
            if (path.Count == 1)
            {
                quantity = 1m;
            }
            else if (occurrence.ParentPath is null || !quantityByPath.TryGetValue(PathKey(occurrence.ParentPath), out var parentQuantity))
            {
                throw new ArgumentException("Every non-root occurrence must refer to an earlier parent path.", nameof(occurrences));
            }
            else if (parentQuantity is null)
            {
                // A failed ancestor already explains why this quantity is unknown.
                quantity = null;
            }
            else
            {
                try
                {
                    quantity = checked(parentQuantity.Value * occurrence.LocalQuantity);
                }
                catch (OverflowException)
                {
                    quantity = null;
                    diagnostics.Add(Diagnostic("QuantityOverflow", occurrence,
                        "The quantity along this composition path exceeds the supported decimal range."));
                }
            }
            quantityByPath[pathKey] = quantity;

            if (occurrence.VersionId is null)
            {
                diagnostics.Add(Diagnostic("NoCurrentVersion", occurrence,
                    "The object has no current non-cancelled version."));
                if (occurrence.Type != PdmObjectType.Assembly)
                    AddOccurrence(groups, occurrence, quantity, diagnostics);
                continue;
            }

            if (occurrence.Type == PdmObjectType.Assembly)
                continue;

            if (occurrence.UnitMassKg is null)
            {
                diagnostics.Add(Diagnostic("MissingMass", occurrence,
                    "The current version does not have a unit mass."));
            }

            AddOccurrence(groups, occurrence, quantity, diagnostics);
        }

        var items = new List<CalculatedSpecificationItem>(groups.Count);
        decimal totalMass = 0m;
        var totalOverflow = false;
        foreach (var group in groups.Values.OrderBy(x => PathKey(x.First.ObjectPath), StringComparer.Ordinal))
        {
            decimal? lineMass = null;
            if (group.Quantity is { } quantity && group.First.VersionId is not null && group.First.UnitMassKg is { } unitMass)
            {
                try
                {
                    lineMass = checked(quantity * unitMass);
                }
                catch (OverflowException)
                {
                    diagnostics.Add(Diagnostic("MassOverflow", group.First,
                        "The total mass for this specification item exceeds the supported decimal range."));
                }
            }

            items.Add(new CalculatedSpecificationItem(group.First.ObjectId, group.First.Type,
                group.First.Designation, group.First.Name, group.First.Material,
                group.First.VersionId, group.First.VersionNumber, group.Quantity,
                group.First.VersionId is null ? null : group.First.UnitMassKg, lineMass));

            if (lineMass is { } knownLineMass && !totalOverflow)
            {
                try
                {
                    totalMass = checked(totalMass + knownLineMass);
                }
                catch (OverflowException)
                {
                    diagnostics.Add(Diagnostic("MassOverflow", group.First,
                        "The calculated assembly mass exceeds the supported decimal range."));
                    totalOverflow = true;
                }
            }
        }

        var complete = diagnostics.Count == 0;
        return new CompositionCalculationResult(rootObjectId, complete ? totalMass : null,
            complete, items, diagnostics);
    }

    private static void AddOccurrence(Dictionary<Guid, SpecificationAccumulator> groups,
        CompositionCalculationInput occurrence, decimal? quantity, List<CalculationDiagnostic> diagnostics)
    {
        if (!groups.TryGetValue(occurrence.ObjectId, out var group))
        {
            groups.Add(occurrence.ObjectId, new SpecificationAccumulator(occurrence, quantity));
            return;
        }

        if (group.Quantity is null || quantity is null)
        {
            group.Quantity = null;
            return;
        }

        try
        {
            group.Quantity = checked(group.Quantity.Value + quantity.Value);
        }
        catch (OverflowException)
        {
            group.Quantity = null;
            diagnostics.Add(Diagnostic("QuantityOverflow", occurrence,
                "The combined specification quantity exceeds the supported decimal range."));
        }
    }

    private static CalculationDiagnostic Diagnostic(string code, CompositionCalculationInput occurrence, string message) =>
        new(code, occurrence.ObjectId, occurrence.ObjectPath.ToArray(), message);

    private static string PathKey(IReadOnlyList<Guid> path) => string.Join("/", path.Select(x => x.ToString("N")));

    private sealed class SpecificationAccumulator(CompositionCalculationInput first, decimal? quantity)
    {
        public CompositionCalculationInput First { get; } = first;
        public decimal? Quantity { get; set; } = quantity;
    }
}
