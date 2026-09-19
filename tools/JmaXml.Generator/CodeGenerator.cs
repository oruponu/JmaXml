using System.Text;

namespace JmaXml.Generator;

public static class CodeGenerator
{
    public static IReadOnlyDictionary<string, string> Run(string schemaDir, TextWriter log)
    {
        if (!Directory.Exists(schemaDir))
        {
            throw new GeneratorException($"{schemaDir}: schema directory not found");
        }
        var dictionaries = Directory.GetFiles(schemaDir, "jmaxml_*_dictionary.xlsx");
        if (dictionaries.Length != 1)
        {
            throw new GeneratorException($"Expected exactly one dictionary xlsx in {schemaDir} but found {dictionaries.Length}");
        }
        var schema = SchemaLoader.Load(Path.Combine(schemaDir, "xsd"));
        var dictionary = DictionaryReader.Read(dictionaries[0]);
        var alignment = Alignment.Check(schema, dictionary);
        foreach (var warning in alignment.Warnings) log.WriteLine($"warning: {warning}");
        if (alignment.Errors.Length > 0) throw new GeneratorException(string.Join(Environment.NewLine, alignment.Errors));
        var naming = Naming.Validate(schema);
        if (naming.Length > 0) throw new GeneratorException(string.Join(Environment.NewLine, naming));
        return CSharpEmitter.Emit(schema, dictionary);
    }

    public static void Write(IReadOnlyDictionary<string, string> files, string outDir)
    {
        Directory.CreateDirectory(outDir);
        foreach (var stale in Directory.GetFiles(outDir, "*.g.cs").Where(f => !files.ContainsKey(Path.GetFileName(f))))
        {
            File.Delete(stale);
        }
        foreach (var (name, content) in files)
        {
            File.WriteAllText(Path.Combine(outDir, name), content, new UTF8Encoding(false));
        }
    }

    public static IReadOnlyList<string> Diff(IReadOnlyDictionary<string, string> files, string outDir)
    {
        var differences = new List<string>();
        foreach (var (name, content) in files)
        {
            var path = Path.Combine(outDir, name);
            if (!File.Exists(path))
            {
                differences.Add($"{name}: missing");
            }
            else if (File.ReadAllText(path).ReplaceLineEndings("\n") != content)
            {
                differences.Add($"{name}: differs");
            }
        }
        if (Directory.Exists(outDir))
        {
            foreach (var stale in Directory.GetFiles(outDir, "*.g.cs").Where(f => !files.ContainsKey(Path.GetFileName(f))))
            {
                differences.Add($"{Path.GetFileName(stale)}: unexpected file");
            }
        }
        return differences;
    }
}
