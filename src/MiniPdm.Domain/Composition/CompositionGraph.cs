namespace MiniPdm.Domain.Composition;

public sealed record CompositionGraphEdge(Guid ParentId, Guid ChildId);

public static class CompositionGraph
{
    public static Guid[]? FindCyclePath(IEnumerable<CompositionGraphEdge> edges)
    {
        var adjacency = edges.GroupBy(x => x.ParentId)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ChildId).Distinct().ToArray());
        var state = new Dictionary<Guid, byte>();
        var stack = new List<Guid>();
        var positions = new Dictionary<Guid, int>();
        foreach (var node in adjacency.Keys.Concat(adjacency.Values.SelectMany(x => x)).Distinct())
        {
            var path = Visit(node);
            if (path is not null) return path;
        }
        return null;

        Guid[]? Visit(Guid node)
        {
            if (state.TryGetValue(node, out var currentState)) return null;
            state[node] = 1;
            positions[node] = stack.Count;
            stack.Add(node);
            if (adjacency.TryGetValue(node, out var children))
            foreach (var child in children)
            {
                if (state.TryGetValue(child, out var childState) && childState == 1)
                    return stack.Skip(positions[child]).Append(child).ToArray();
                if (childState == 0)
                {
                    var path = Visit(child);
                    if (path is not null) return path;
                }
            }
            stack.RemoveAt(stack.Count - 1);
            positions.Remove(node);
            state[node] = 2;
            return null;
        }
    }

    public static IReadOnlySet<Guid> FindCycleNodes(IEnumerable<CompositionGraphEdge> edges)
    {
        var edgeArray = edges.ToArray();
        var adjacency = edgeArray.GroupBy(x => x.ParentId)
            .ToDictionary(x => x.Key, x => x.Select(e => e.ChildId).Distinct().ToArray());
        var nodes = edgeArray.SelectMany(x => new[] { x.ParentId, x.ChildId }).Distinct().ToArray();
        var cyclic = new HashSet<Guid>();
        foreach (var start in nodes)
        {
            var seen = new HashSet<Guid>();
            var pending = new Stack<Guid>();
            if (adjacency.TryGetValue(start, out var children)) foreach (var child in children) pending.Push(child);
            while (pending.TryPop(out var current))
            {
                if (current == start) { cyclic.Add(start); break; }
                if (!seen.Add(current)) continue;
                if (adjacency.TryGetValue(current, out var next)) foreach (var child in next) pending.Push(child);
            }
        }
        return cyclic;
    }
}
