using System.Xml;
using System.Xml.Schema;

namespace XAFtoolkit.Auditfile.Generator.Schema;

/// <summary>Loads and compiles an XSD. DTDs are refused; includes may only come from the source folder.</summary>
internal static class XsdLoader
{
    public static XmlSchemaElement LoadRoot(string xsdPath, string rootElement)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(xsdPath))!;
        var resolver = new FolderResolver(folder);
        var errors = new List<string>();

        var set = new XmlSchemaSet { XmlResolver = resolver };
        set.ValidationEventHandler += (_, e) => errors.Add($"{e.Severity}: {e.Message}");

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = resolver };
        using (var reader = XmlReader.Create(xsdPath, settings))
        {
            set.Add(null, reader);
        }

        set.Compile();
        if (errors.Count > 0)
        {
            throw new GeneratorException($"XSD does not compile: {string.Join("; ", errors)}");
        }

        return set.GlobalElements.Values.Cast<XmlSchemaElement>().SingleOrDefault(e => e.QualifiedName.Name == rootElement)
            ?? throw new GeneratorException($"root element '{rootElement}' not found in {Path.GetFileName(xsdPath)}");
    }

    /// <summary>Resolves only files inside one folder. Anything else (other folders, http) is refused.</summary>
    private sealed class FolderResolver(string folder) : XmlUrlResolver
    {
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            var path = absoluteUri.IsFile ? Path.GetFullPath(absoluteUri.LocalPath) : null;
            var inside = path is not null
                && path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            return inside
                ? base.GetEntity(absoluteUri, role, ofObjectToReturn)
                : throw new GeneratorException($"refused to resolve '{absoluteUri}' outside the source folder");
        }
    }
}
