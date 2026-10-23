namespace XAFtoolkit.Auditfile.Generator.Mapping;

// Output model of manifest.json. Property order here is the order in the file.

internal sealed record Manifest(
    int FormatVersion,
    string Standard,
    string TypeName,
    string Version,
    string Namespace,
    string RootElement,
    string Provenance,
    ManifestXsd Xsd,
    string GeneratorVersion,
    IReadOnlyList<string> RequiredTables,
    IReadOnlyList<string> ExpectedTables,
    IReadOnlyList<ManifestTable> Tables);

internal sealed record ManifestXsd(string File, string Sha256);

/// <summary>MinOccurs/MaxOccurs are relative to the XML parent element; MaxOccurs null means unbounded.</summary>
internal sealed record ManifestTable(
    string Name,
    string XmlName,
    string Path,
    string? Parent,
    int MinOccurs,
    int? MaxOccurs,
    IReadOnlyList<ManifestKey>? Keys,
    IReadOnlyList<ManifestReference>? References,
    IReadOnlyList<ManifestColumn> Columns);

/// <summary>Path is relative to the table element: "accID", "changeInfo/userID", "@attr", or "." for a value table.</summary>
internal sealed record ManifestColumn(
    string Name,
    string XmlName,
    string Path,
    string Kind,
    string Type,
    string XsdType,
    int MinOccurs);

internal sealed record ManifestKey(string Name, IReadOnlyList<string> Columns);

internal sealed record ManifestReference(string Name, IReadOnlyList<string> Columns, string Table, string Key);
