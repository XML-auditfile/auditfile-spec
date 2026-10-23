using System.Xml.Schema;

namespace XAFtoolkit.Auditfile.Generator.Schema;

/// <summary>
/// One element of the XSD, independent of System.Xml.Schema.
/// MinOccurs and MaxOccurs are effective values relative to the XML parent element
/// (including enclosing sequence/choice); MaxOccurs null means unbounded.
/// </summary>
internal sealed class SchemaNode
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required decimal MinOccurs { get; init; }
    public required decimal? MaxOccurs { get; init; }

    /// <summary>Simple content type, or null for an element that only has child elements.</summary>
    public SimpleTypeInfo? Text { get; init; }

    public List<SchemaAttribute> Attributes { get; } = [];
    public List<SchemaNode> Children { get; } = [];

    public bool IsComplex => Children.Count > 0 || Attributes.Count > 0;
    public bool Repeats => MaxOccurs is null || MaxOccurs > 1;
}

internal sealed record SchemaAttribute(string Name, SimpleTypeInfo Type, bool Required);

internal sealed record SimpleTypeInfo(XmlTypeCode TypeCode, string XsdTypeName);

/// <summary>xs:key, xs:unique or xs:keyref as declared in the XSD.</summary>
internal sealed record SchemaConstraint(
    string Kind,
    string Name,
    string DeclaredOn,
    string Selector,
    IReadOnlyList<string> Fields,
    string? Refer);
