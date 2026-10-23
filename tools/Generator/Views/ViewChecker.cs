using XAFtoolkit.Auditfile.Generator.Mapping;

namespace XAFtoolkit.Auditfile.Generator.Views;

/// <summary>
/// Decides from the manifest alone whether a view can be built for one version. Readers apply the same rules
/// and report XAF-Q001 with the first missing item when a view is requested that is not available.
/// </summary>
internal static class ViewChecker
{
    /// <summary>What the manifest lacks for this view; empty means the view is available.</summary>
    public static IReadOnlyList<string> Missing(View view, Manifest manifest)
    {
        var tables = manifest.Tables.ToDictionary(t => t.Name, StringComparer.Ordinal);
        if (!tables.TryGetValue(view.Source, out var source))
        {
            return [$"table {view.Source}"];
        }

        var missing = new List<string>();
        foreach (var join in view.Joins ?? [])
        {
            if (!tables.ContainsKey(join.Table))
            {
                missing.Add($"table {join.Table}");
            }
            else if (join.On is null && !IsAncestor(tables, source, join.Table))
            {
                missing.Add($"table {join.Table} as ancestor of {view.Source}");
            }
        }

        foreach (var reference in view.References())
        {
            if (ColumnReference.TryParse(reference, out var table, out var column)
                && tables.TryGetValue(table, out var t) && !t.Columns.Any(c => c.Name == column))
            {
                missing.Add($"column {reference}");
            }
        }

        return missing;
    }

    private static bool IsAncestor(Dictionary<string, ManifestTable> tables, ManifestTable table, string ancestor)
    {
        for (var parent = table.Parent; parent is not null; parent = tables[parent].Parent)
        {
            if (parent == ancestor)
            {
                return true;
            }
        }

        return false;
    }
}
