namespace XAFtoolkit.Auditfile.Generator.Output;

/// <summary>The complete contents of generated/, built in memory before anything is written.</summary>
internal sealed class GeneratedFiles
{
    private readonly SortedDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public int Count => _files.Count;

    public void Add(string relativePath, byte[] content)
    {
        if (!_files.TryAdd(relativePath, content))
        {
            throw new GeneratorException($"generated file written twice: {relativePath}");
        }
    }

    /// <summary>Replaces the target folder completely, so stale files never survive.</summary>
    public void WriteTo(string target)
    {
        if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }

        foreach (var (relative, content) in _files)
        {
            var path = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
        }
    }

    /// <summary>Lists files that are missing, different or superfluous in the target folder.</summary>
    public List<string> Compare(string target)
    {
        var differences = new List<string>();
        var existing = Directory.Exists(target)
            ? Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(target, f).Replace('\\', '/'))
                .ToHashSet(StringComparer.Ordinal)
            : [];

        foreach (var (relative, content) in _files)
        {
            if (!existing.Remove(relative))
            {
                differences.Add($"{relative} (missing)");
            }
            else if (!File.ReadAllBytes(Path.Combine(target, relative)).AsSpan().SequenceEqual(content))
            {
                differences.Add($"{relative} (changed)");
            }
        }

        differences.AddRange(existing.Order(StringComparer.Ordinal).Select(f => $"{f} (not generated)"));
        return differences;
    }
}
