using XAFtoolkit.Auditfile.Generator.Config;
using XAFtoolkit.Auditfile.Generator.Mapping;
using XAFtoolkit.Auditfile.Generator.Output;
using XAFtoolkit.Auditfile.Generator.Views;

namespace XAFtoolkit.Auditfile.Generator;

/// <summary>
/// Entry point. Usage: generator [--root &lt;path&gt;] [--check]
/// Without --check the generated/ folder is rewritten; with --check it is compared and left untouched.
/// Exit codes: 0 = ok, 1 = validation error or differences, 2 = unexpected failure.
/// </summary>
internal static class Cli
{
    public static int Run(string[] args)
    {
        try
        {
            var check = args.Contains("--check");
            // Use the specified root or the current directory if not provided.
            var root = FindRoot(GetOption(args, "--root") ?? Directory.GetCurrentDirectory());

            // Load the generator configuration from the root folder.
            var config = GeneratorConfig.Load(Path.Combine(root, "generator.json"));

            // Prepare containers for generated files, registry entries, and manifests.
            var files = new GeneratedFiles();
            var registry = new List<RegistryEntry>();
            var manifests = new List<Manifest>();

            // Generate each standard and collect the resulting registry entries and manifests.
            foreach (var standard in config.Standards)
            {
                Console.WriteLine($"{standard.Type}/{standard.Version}");
                var (entry, manifest) = StandardGenerator.Generate(root, standard, files);
                registry.Add(entry);
                manifests.Add(manifest);
            }
            // Generate views based on the collected manifests.
            ViewGenerator.Generate(root, config.Views ?? [], manifests, files);

            // Add shared output based on the generated files and registry entries.
            OutputWriter.AddShared(root, config, registry, files);

            var target = Path.Combine(root, "generated");
            if (check)
            {
                var differences = files.Compare(target);
                foreach (var difference in differences)
                {
                    Console.Error.WriteLine($"  differs: {difference}");
                }

                Console.WriteLine(differences.Count == 0 ? "generated/ is up to date" : "generated/ is out of date");
                return differences.Count == 0 ? 0 : 1;
            }

            files.WriteTo(target);
            Console.WriteLine($"wrote {files.Count} files to generated/");
            return 0;
        }
        catch (GeneratorException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"unexpected: {ex}");
            return 2;
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "generator.json")))
            {
                return dir.FullName;
            }
        }

        throw new GeneratorException($"generator.json not found in '{start}' or any parent folder");
    }
}
