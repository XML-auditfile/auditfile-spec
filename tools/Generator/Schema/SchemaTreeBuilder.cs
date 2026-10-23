using System.Xml.Schema;

namespace XAFtoolkit.Auditfile.Generator.Schema;

/// <summary>Walks the compiled XSD from the root element and builds a SchemaNode tree.</summary>
internal sealed class SchemaTreeBuilder
{
    private const int MaxDepth = 64;

    public List<SchemaConstraint> Constraints { get; } = [];

    public SchemaNode Build(XmlSchemaElement root) => BuildElement(root, parentPath: null, 1m, 1m, depth: 0);

    private SchemaNode BuildElement(XmlSchemaElement element, string? parentPath, decimal minFactor, decimal? maxFactor, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new GeneratorException($"nesting deeper than {MaxDepth} at {parentPath} (recursive type?)");
        }

        var name = element.QualifiedName.Name;
        var path = parentPath is null ? name : $"{parentPath}/{name}";
        var max = maxFactor is null || element.MaxOccurs == decimal.MaxValue ? (decimal?)null : maxFactor * element.MaxOccurs;

        SimpleTypeInfo? text = element.ElementSchemaType switch
        {
            XmlSchemaSimpleType simple => Describe(simple),
            XmlSchemaComplexType { ContentType: XmlSchemaContentType.TextOnly } complex => new(complex.Datatype!.TypeCode, complex.Datatype.TypeCode.ToString()),
            _ => null,
        };

        var node = new SchemaNode { Name = name, Path = path, MinOccurs = minFactor * element.MinOccurs, MaxOccurs = max, Text = text };

        if (element.ElementSchemaType is XmlSchemaComplexType type)
        {
            foreach (var attribute in type.AttributeUses.Values.Cast<XmlSchemaAttribute>().OrderBy(a => a.QualifiedName.Name, StringComparer.Ordinal))
            {
                node.Attributes.Add(new(attribute.QualifiedName.Name, Describe(attribute.AttributeSchemaType!), attribute.Use == XmlSchemaUse.Required));
            }

            AddParticle(node, type.ContentTypeParticle, 1m, 1m, depth);
        }

        foreach (var constraint in element.Constraints.Cast<XmlSchemaIdentityConstraint>())
        {
            Constraints.Add(new(
                constraint switch { XmlSchemaKeyref => "keyref", XmlSchemaUnique => "unique", _ => "key" },
                constraint.Name!,
                path,
                constraint.Selector!.XPath!,
                constraint.Fields.Cast<XmlSchemaXPath>().Select(f => f.XPath!).ToList(),
                (constraint as XmlSchemaKeyref)?.Refer.Name));
        }

        return node;
    }

    private void AddParticle(SchemaNode parent, XmlSchemaParticle particle, decimal minFactor, decimal? maxFactor, int depth)
    {
        switch (particle)
        {
            case XmlSchemaElement element:
                parent.Children.Add(BuildElement(element, parent.Path, minFactor, maxFactor, depth + 1));
                break;

            case XmlSchemaGroupBase group:
                var min = group is XmlSchemaChoice ? 0m : minFactor * group.MinOccurs;
                var max = maxFactor is null || group.MaxOccurs == decimal.MaxValue ? (decimal?)null : maxFactor * group.MaxOccurs;
                foreach (var item in group.Items.Cast<XmlSchemaParticle>())
                {
                    AddParticle(parent, item, min, max, depth);
                }

                break;

            case XmlSchemaAny:
                throw new GeneratorException($"xs:any is not supported ({parent.Path})");
        }
    }

    private static SimpleTypeInfo Describe(XmlSchemaSimpleType type) =>
        new(type.Datatype!.TypeCode, type.QualifiedName.IsEmpty ? type.Datatype.TypeCode.ToString() : type.QualifiedName.Name);
}
