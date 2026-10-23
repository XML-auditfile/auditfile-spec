using System.Security.Cryptography;

namespace XAFtoolkit.Auditfile.Generator.Config;

/// <summary>Checks that every official source exists and matches its SHA-256 in generator.json.</summary>
internal static class SourceVerifier
{
    public static void Verify(string root, StandardConfig standard)
    {
        var official = new List<string> { standard.Sources.Xsd.Main };
        official.AddRange(standard.Sources.Xsd.Includes ?? []);
        official.AddRange(standard.Sources.Documents ?? []);

        foreach (var file in official)
        {
            if (!standard.Checksums.ContainsKey(file))
            {
                throw new GeneratorException($"{standard.Key}: no checksum for '{file}'");
            }
        }

        foreach (var (file, expected) in standard.Checksums)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
            {
                throw new GeneratorException($"{standard.Key}: source not found: {file}");
            }

            var actual = Sha256(File.ReadAllBytes(path));
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new GeneratorException($"{standard.Key}: checksum mismatch for {file} (actual {actual})");
            }
        }
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
