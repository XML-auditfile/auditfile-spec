namespace XAFtoolkit.Auditfile.Generator.Mapping;

/// <summary>
/// Table names: a name from overlay.json wins; otherwise the snake_case element name.
/// When names collide, each colliding table takes one more path segment as prefix until all are unique,
/// e.g. "vat" → "tr_line_vat" and "sb_line_vat".
/// </summary>
internal static class TableNamer
{
    public static void Assign(IReadOnlyList<TableBuilder> tables, IReadOnlyDictionary<string, string> fixedNames)
    {
        var unknown = fixedNames.Keys.Except(tables.Select(t => t.Path)).ToList();
        if (unknown.Count > 0)
        {
            throw new GeneratorException($"overlay.json names unknown table paths: {string.Join(", ", unknown)}");
        }

        var depth = tables.ToDictionary(t => t, _ => 1);
        foreach (var table in tables)
        {
            table.Name = fixedNames.TryGetValue(table.Path, out var name) ? name : Suffix(table.Path, 1);
        }

        while (true)
        {
            var colliding = tables.GroupBy(t => t.Name).Where(g => g.Count() > 1).SelectMany(g => g)
                .Where(t => !fixedNames.ContainsKey(t.Path)).ToList();

            if (colliding.Count == 0)
            {
                break;
            }

            foreach (var table in colliding)
            {
                var segments = table.Path.Split('/').Length;
                if (++depth[table] > segments)
                {
                    throw new GeneratorException($"cannot find a unique name for {table.Path}; set one in overlay.json");
                }

                table.Name = Suffix(table.Path, depth[table]);
            }
        }

        var clash = tables.GroupBy(t => t.Name).FirstOrDefault(g => g.Count() > 1);
        if (clash is not null)
        {
            throw new GeneratorException($"table name '{clash.Key}' is used twice; check overlay.json");
        }

        foreach (var table in tables)
        {
            Naming.EnsureLength(table.Name, table.Path);
        }
    }

    private static string Suffix(string path, int count)
    {
        var segments = path.Split('/');
        return string.Join("_", segments[^count..].Select(Naming.Snake));
    }
}
