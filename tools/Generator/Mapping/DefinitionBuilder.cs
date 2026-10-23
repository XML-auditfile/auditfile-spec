using System.Text.Json;
using System.Text.Json.Nodes;

namespace XAFtoolkit.Auditfile.Generator.Mapping;

/// <summary>
/// Builds definition.json from the hand-written definitions.json (keyed by XML path)
/// and the mapping (XML path → table, column or group). Unknown paths are an error.
/// </summary>
internal static class DefinitionBuilder
{
    public static JsonObject Build(string root, string? relativePath, IReadOnlyList<MappedPath> paths, string standard, string version)
    {
        var output = new JsonObject
        {
            ["formatVersion"] = 1,
            ["standard"] = standard,
            ["version"] = version,
        };
        var entries = new JsonArray();
        output["entries"] = entries;

        if (relativePath is null)
        {
            return output;
        }

        var file = Path.Combine(root, relativePath);
        if (!File.Exists(file))
        {
            throw new GeneratorException($"definitions not found: {relativePath}");
        }

        var source = JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip })
            ?? throw new GeneratorException($"{relativePath} is empty");

        var byPath = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var entry in source["entries"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            var path = entry["path"]?.GetValue<string>() ?? throw new GeneratorException($"{relativePath}: entry without path");
            if (!byPath.TryAdd(path, entry))
            {
                throw new GeneratorException($"{relativePath}: duplicate path {path}");
            }
        }

        var known = paths.Select(p => p.Path).ToHashSet(StringComparer.Ordinal);
        var unknown = byPath.Keys.Where(p => !known.Contains(p)).ToList();
        if (unknown.Count > 0)
        {
            throw new GeneratorException($"{relativePath}: unknown paths: {string.Join(", ", unknown.Take(5))}");
        }

        foreach (var mapped in paths)
        {
            if (!byPath.TryGetValue(mapped.Path, out var entry))
            {
                continue;
            }

            var (key, kind) = mapped switch
            {
                { Column: not null } => ($"{mapped.Table.Name}.{mapped.Column.Name}", "column"),
                { Group: not null } => ($"{mapped.Table.Name}.{mapped.Group}", "group"),
                _ => (mapped.Table.Name, "table"),
            };

            var result = new JsonObject { ["key"] = key, ["kind"] = kind };
            foreach (var property in new[] { "label", "definition", "description", "codes" })
            {
                if (entry[property] is { } value)
                {
                    result[property] = value.DeepClone();
                }
            }

            entries.Add(result);
        }

        return output;
    }
}
