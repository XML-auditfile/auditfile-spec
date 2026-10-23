using System.Text.Json.Nodes;
using XAFtoolkit.Auditfile.Generator.Config;

namespace XAFtoolkit.Auditfile.Generator.Output;

/// <summary>Files in generated/ that are shared by all standards: registry.json and diagnostics.json.</summary>
internal static class OutputWriter
{
    public static void AddShared(string root, GeneratorConfig config, List<RegistryEntry> registry, GeneratedFiles files)
    {
        files.Add("registry.json", Json.Serialize(new Registry(1, registry)));

        files.Add("diagnostics.json", config.Diagnostics is { } diagnostics
            ? Json.Normalize(Path.Combine(root, diagnostics))
            : Json.Serialize(new JsonObject { ["formatVersion"] = 1, ["codes"] = new JsonArray() }));
    }
}
