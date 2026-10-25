using System.Text.RegularExpressions;
using XAFtoolkit.Auditfile.Generator.Mapping;
using XAFtoolkit.Auditfile.Generator.Output;

namespace XAFtoolkit.Auditfile.Generator.Views;

/// <summary>
/// Checks every views.json and copies it to generated/views/&lt;type&gt;.json. A view must be well-formed and
/// available in at least one version of its type; which versions is decided by readers, from the manifest.
/// </summary>
internal static partial class ViewGenerator
{
    private const int FormatVersion = 1;
    private static readonly HashSet<string> Aggregates = ["sum", "count", "min", "max"];
    private static readonly HashSet<string> Operators = ["=", ">=", "<="];

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex Name();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex Prefix();

    public static void Generate(string root, IReadOnlyList<string> paths, IReadOnlyList<Manifest> manifests, GeneratedFiles files)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var file = ViewFile.Load(root, path);
            if (file.FormatVersion != FormatVersion)
            {
                throw new GeneratorException($"{path}: unknown formatVersion {file.FormatVersion}");
            }

            var versions = manifests.Where(m => m.Standard == file.Type).ToList();
            if (versions.Count == 0)
            {
                throw new GeneratorException($"{path}: type '{file.Type}' is not in generator.json");
            }

            if (!types.Add(file.Type))
            {
                throw new GeneratorException($"{path}: second views file for type {file.Type}");
            }

            Console.WriteLine($"views/{file.Type}");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var view in file.Views ?? [])
            {
                Validate(path, view);
                if (!names.Add(view.Name))
                {
                    throw new GeneratorException($"{path}: duplicate view {view.Name}");
                }

                Report(path, view, versions);
            }

            files.Add($"views/{file.Type}.json", Json.Normalize(Path.Combine(root, path)));
        }
    }

    private static void Report(string path, View view, List<Manifest> versions)
    {
        var missing = versions.Select(m => (m.Version, Missing: ViewChecker.Missing(view, m))).ToList();
        var unavailable = missing.Where(m => m.Missing.Count > 0).Select(m => $"{m.Version}: {string.Join(", ", m.Missing)}").ToList();
        if (unavailable.Count == versions.Count)
        {
            throw new GeneratorException($"{path}: view {view.Name} is not available in any version ({string.Join("; ", unavailable)})");
        }

        Console.WriteLine(unavailable.Count == 0
            ? $"  {view.Name}: all versions"
            : $"  {view.Name}: not in {string.Join("; ", unavailable)}");
    }

    private static void Validate(string path, View view)
    {
        if (view.Name is null || !Name().IsMatch(view.Name))
        {
            throw new GeneratorException($"{path}: invalid view name '{view.Name}'");
        }

        void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new GeneratorException($"{path}: view {view.Name}: {message}");
            }
        }

        Check(!string.IsNullOrEmpty(view.Label?.GetValueOrDefault("nl")), "label.nl is required");
        Check(!string.IsNullOrEmpty(view.Source), "source is required");

        // Tables in scope, in join order: an "on" may only use the source and earlier joins.
        var scope = new List<string> { view.Source };
        foreach (var join in view.Joins ?? [])
        {
            Check(!string.IsNullOrEmpty(join.Table) && !scope.Contains(join.Table), $"join '{join.Table}' is empty or repeated");
            Check(join.Alternatives is null || (join.Alternatives.Count > 0 && join.Alternatives.All(a => !string.IsNullOrEmpty(a) && a != join.Table)),
                $"join {join.Table}: 'alternatives' is empty or names the table itself");
            foreach (var pair in join.On ?? [])
            {
                Check(pair.Count == 2, $"join {join.Table}: each 'on' entry is [earlier column, joined column]");
                Check(InScope(pair[0], scope), $"join {join.Table}: '{pair[0]}' is not a column of an earlier table");
                Check(ColumnReference.TryParse(pair[1], out var table, out _) && table == join.Table,
                    $"join {join.Table}: '{pair[1]}' is not a column of {join.Table}");
            }

            Check(join.On is null || join.On.Count > 0, $"join {join.Table}: 'on' is empty");
            scope.Add(join.Table);
        }

        var parameters = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in view.Parameters ?? [])
        {
            Check(parameter.Name is not null && Name().IsMatch(parameter.Name) && parameters.Add(parameter.Name),
                $"parameter '{parameter.Name}' is invalid or repeated");
            Check(!string.IsNullOrEmpty(parameter.Label?.GetValueOrDefault("nl")), $"parameter {parameter.Name}: label.nl is required");
            Check(InScope(parameter.Column, scope), $"parameter {parameter.Name}: '{parameter.Column}' is not a column of a view table");
            Check(parameter.Operator is not null && Operators.Contains(parameter.Operator),
                $"parameter {parameter.Name}: operator must be one of {string.Join(" ", Operators)}");
        }

        Check(view.Columns is { Count: > 0 }, "at least one column is required");
        var columns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in view.Columns!)
        {
            Check(column.Name is not null && Name().IsMatch(column.Name) && columns.Add(column.Name),
                $"column '{column.Name}' is invalid or repeated");
            Check(!string.IsNullOrEmpty(column.Label?.GetValueOrDefault("nl")), $"column {column.Name}: label.nl is required");
            Check(column.Aggregate is null || Aggregates.Contains(column.Aggregate),
                $"column {column.Name}: aggregate must be one of {string.Join(" ", Aggregates)}");
            Check(column.Value is not null || column.Aggregate == "count", $"column {column.Name}: value is required");

            if (column.Value is { } value)
            {
                Check(value.References().All(r => InScope(r, scope)), $"column {column.Name}: value uses a column outside the view tables");
                Check(value.Op switch
                {
                    null => value.Negative is null && value.In is null && value.Prefix is null,
                    "signed" => value.By is not null && value.Negative is { Count: > 0 } && value.In is null && value.Prefix is null,
                    "only" => value.By is not null && value.In is { Count: > 0 } && value.Negative is null && value.Prefix is null,
                    "pseudonym" => value.By is null && value.In is null && value.Negative is null && value.Prefix is not null && Prefix().IsMatch(value.Prefix),
                    _ => false,
                }, $"column {column.Name}: op must be 'signed' (value, by, negative), 'only' (value, by, in) or 'pseudonym' (value, prefix)");
                Check(value.Op != "pseudonym" || column.Aggregate is null or "count", $"column {column.Name}: a pseudonym cannot be summed, nor have a minimum or maximum");
            }
        }

        foreach (var name in view.OrderBy ?? [])
        {
            Check(columns.Contains(name), $"orderBy '{name}' is not a column of the view");
        }
    }

    private static bool InScope(string? reference, List<string> scope) =>
        ColumnReference.TryParse(reference, out var table, out _) && scope.Contains(table);
}
