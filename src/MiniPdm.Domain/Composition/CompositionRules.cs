namespace MiniPdm.Domain.Composition;

public sealed record CompositionNormalization<TKey>(IReadOnlyDictionary<TKey, int> Items,
    IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) where TKey : notnull;

public static class CompositionRules
{
    public static CompositionNormalization<TKey> Normalize<TKey>(IEnumerable<(TKey Child, int Quantity)> rows)
        where TKey : notnull
    {
        var items = new Dictionary<TKey, int>();
        var errors = new List<string>();
        var repeatedValidRows = false;
        foreach (var (child, quantity) in rows)
        {
            if (quantity <= 0)
            {
                errors.Add($"Component '{child}' must have a positive count.");
                continue;
            }

            if (items.TryGetValue(child, out var existing))
            {
                repeatedValidRows = true;
                try { items[child] = checked(existing + quantity); }
                catch (OverflowException)
                {
                    errors.Add($"The combined count for component '{child}' exceeds Int32.");
                    items.Remove(child);
                }
            }
            else items.Add(child, quantity);
        }

        return new CompositionNormalization<TKey>(items, errors,
            repeatedValidRows && errors.Count == 0 ? ["Repeated component rows were combined."] : []);
    }
}
