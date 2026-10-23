using System.Text.Json.Nodes;
using XAFtoolkit.Auditfile.Generator.Config;
using XAFtoolkit.Auditfile.Generator.Mapping;
using XAFtoolkit.Auditfile.Generator.Output;
using XAFtoolkit.Auditfile.Generator.Schema;

namespace XAFtoolkit.Auditfile.Generator;

/// <summary>Generates all files for one audit file type and version into generated/standards/&lt;type&gt;/&lt;version&gt;/.</summary>
internal static class StandardGenerator
{
    public const string Version = "0.1.0";
    private const int FormatVersion = 1;

    public static (RegistryEntry Entry, Manifest Manifest) Generate(string root, StandardConfig standard, GeneratedFiles files)
    {
        SourceVerifier.Verify(root, standard);

        var xsdPath = Path.Combine(root, standard.Sources.Xsd.Main);
        var rootElement = XsdLoader.LoadRoot(xsdPath, standard.RootElement);
        var tree = new SchemaTreeBuilder();
        var node = tree.Build(rootElement);

        var overlay = Overlay.Load(root, standard.Sources.Overlay);
        var mapper = TableMapper.Map(node, overlay.TableNames ?? new Dictionary<string, string>());
        RelationMapper.Apply(mapper.Tables, tree.Constraints);

        var xsdBytes = File.ReadAllBytes(xsdPath);
        var manifest = new Manifest(
            FormatVersion,
            standard.Type,
            standard.TypeName,
            standard.Version,
            rootElement.QualifiedName.Namespace,
            standard.RootElement,
            standard.Provenance,
            new ManifestXsd("auditfile.xsd", SourceVerifier.Sha256(xsdBytes)),
            Version,
            mapper.Tables.Where(t => t.Required).Select(t => t.Name).ToList(),
            ResolveExpected(mapper, overlay),
            mapper.Tables.Select(t => new ManifestTable(
                t.Name,
                t.XmlName,
                t.Path,
                t.Parent?.Name,
                t.MinOccurs,
                t.MaxOccurs,
                t.Keys.Count > 0 ? t.Keys : null,
                t.References.Count > 0 ? t.References : null,
                t.Columns)).ToList());

        var folder = standard.Folder;
        files.Add($"{folder}/auditfile.xsd", xsdBytes);
        foreach (var include in standard.Sources.Xsd.Includes ?? [])
        {
            files.Add($"{folder}/{Path.GetFileName(include)}", File.ReadAllBytes(Path.Combine(root, include)));
        }

        files.Add($"{folder}/manifest.json", Json.Serialize(manifest));
        files.Add($"{folder}/definition.json", Json.Serialize(
            DefinitionBuilder.Build(root, standard.Sources.Definitions, mapper.Paths, standard.Type, standard.Version)));
        files.Add($"{folder}/dialects.json", standard.Sources.Dialects is { } dialects
            ? Json.Normalize(Path.Combine(root, dialects))
            : Json.Serialize(new JsonObject { ["formatVersion"] = FormatVersion, ["dialects"] = new JsonArray() }));

        Console.WriteLine($"  {mapper.Tables.Count} tables, {mapper.Tables.Sum(t => t.Columns.Count)} columns, " +
                          $"{mapper.Tables.Sum(t => t.Keys.Count)} keys, {mapper.Tables.Sum(t => t.References.Count)} references");

        var namespaces = new List<RegistryNamespace> { new(rootElement.QualifiedName.Namespace, "official", null, null) };
        namespaces.AddRange((overlay.Aliases ?? []).Select(a => new RegistryNamespace(a.Namespace, "alias", a.Package, a.TestCase)));

        var entry = new RegistryEntry(
            standard.Key,
            standard.Type,
            standard.TypeName,
            standard.Version,
            folder,
            standard.RootElement,
            standard.Provenance,
            namespaces,
            standard.Origin is { } o ? new RegistryOrigin(o.Publisher, o.Url, o.Published) : null);
        return (entry, manifest);
    }

    private static List<string> ResolveExpected(TableMapper mapper, Overlay overlay)
    {
        var byPath = mapper.Tables.ToDictionary(t => t.Path, StringComparer.Ordinal);
        return (overlay.ExpectedTables ?? []).Select(path => byPath.TryGetValue(path, out var table)
            ? table.Name
            : throw new GeneratorException($"overlay.json expects unknown table path {path}")).ToList();
    }
}
