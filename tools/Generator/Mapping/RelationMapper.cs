using XAFtoolkit.Auditfile.Generator.Schema;

namespace XAFtoolkit.Auditfile.Generator.Mapping;

/// <summary>Turns xs:key / xs:unique / xs:keyref into keys and references between tables.</summary>
internal static class RelationMapper
{
    public static void Apply(IReadOnlyList<TableBuilder> tables, IEnumerable<SchemaConstraint> constraints)
    {
        var byPath = tables.ToDictionary(t => t.Path, StringComparer.Ordinal);
        var keys = new Dictionary<string, (TableBuilder Table, ManifestKey Key)>(StringComparer.Ordinal);
        var list = constraints.ToList();

        foreach (var c in list.Where(c => c.Kind is "key" or "unique"))
        {
            var (table, columns) = Resolve(byPath, c);
            var key = new ManifestKey(c.Name, columns);
            table.Keys.Add(key);
            keys[c.Name] = (table, key);
        }

        foreach (var c in list.Where(c => c.Kind == "keyref"))
        {
            var (table, columns) = Resolve(byPath, c);
            if (!keys.TryGetValue(c.Refer!, out var target))
            {
                throw new GeneratorException($"keyref '{c.Name}' refers to unknown key '{c.Refer}'");
            }

            table.References.Add(new(c.Name, columns, target.Table.Name, target.Key.Name));
        }
    }

    private static (TableBuilder Table, List<string> Columns) Resolve(Dictionary<string, TableBuilder> byPath, SchemaConstraint c)
    {
        var selector = Normalize(c.Selector);
        if (selector.Contains('|') || selector.Contains("//", StringComparison.Ordinal))
        {
            throw new GeneratorException($"{c.Kind} '{c.Name}': selector '{c.Selector}' is not supported");
        }

        // A selector may point into a flattened group; then the fields are relative to the nearest table above it.
        var path = selector.Length == 0 ? c.DeclaredOn : $"{c.DeclaredOn}/{selector}";
        var group = "";
        TableBuilder? table;
        while (!byPath.TryGetValue(path, out table))
        {
            var slash = path.LastIndexOf('/');
            if (slash <= c.DeclaredOn.Length)
            {
                throw new GeneratorException($"{c.Kind} '{c.Name}': selector '{c.Selector}' does not point to a table");
            }

            group = $"{path[(slash + 1)..]}/{group}";
            path = path[..slash];
        }

        var columns = c.Fields.Select(field =>
        {
            var relative = group + Normalize(field);
            var column = table.Columns.FirstOrDefault(col => col.Path == relative)
                ?? throw new GeneratorException($"{c.Kind} '{c.Name}': field '{field}' is not a column of {table.Name}");
            return column.Name;
        }).ToList();

        return (table, columns);
    }

    /// <summary>"./x:company/x:vatCodes" → "company/vatCodes".</summary>
    private static string Normalize(string xpath)
    {
        var path = xpath.Trim();
        if (path.StartsWith("./", StringComparison.Ordinal))
        {
            path = path[2..];
        }
        else if (path == ".")
        {
            path = "";
        }

        return string.Join("/", path.Split('/').Select(s => s.Contains(':') ? s[(s.IndexOf(':') + 1)..] : s));
    }
}
