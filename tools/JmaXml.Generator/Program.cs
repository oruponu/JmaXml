using JmaXml.Generator;
using System.Xml;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] is "--schema" or "--out")
    {
        if (++i >= args.Length)
        {
            Console.Error.WriteLine($"Missing value for {args[i - 1]}");
            return 2;
        }
        continue;
    }
    if (args[i] == "--check") continue;
    Console.Error.WriteLine($"Unknown argument: {args[i]}");
    return 2;
}

var schemaDir = Option("--schema") ?? "schema";
var outDir = Option("--out") ?? Path.Combine("src", "JmaXml", "Generated");
try
{
    var files = CodeGenerator.Run(schemaDir, Console.Error);
    if (args.Contains("--check"))
    {
        var differences = CodeGenerator.Diff(files, outDir);
        foreach (var difference in differences) Console.Error.WriteLine(difference);
        return differences.Count == 0 ? 0 : 1;
    }
    CodeGenerator.Write(files, outDir);
    Console.WriteLine($"Generated {files.Count} files into {outDir}");
    return 0;
}
catch (Exception e) when (e is GeneratorException or SchemaException or InvalidDataException or IOException or XmlException)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
