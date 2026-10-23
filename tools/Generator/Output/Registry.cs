namespace XAFtoolkit.Auditfile.Generator.Output;

// Output model of registry.json.

internal sealed record Registry(int FormatVersion, IReadOnlyList<RegistryEntry> Standards);

internal sealed record RegistryEntry(
    string Key,
    string Type,
    string TypeName,
    string Version,
    string Folder,
    string RootElement,
    string Provenance,
    IReadOnlyList<RegistryNamespace> Namespaces,
    RegistryOrigin? Origin);

internal sealed record RegistryNamespace(string Uri, string Kind, string? Package, string? TestCase);

internal sealed record RegistryOrigin(string? Publisher, string? Url, string? Published);
