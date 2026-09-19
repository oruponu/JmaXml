using JmaXml.Generator;

namespace JmaXml.Tests;

internal static class TestXsd
{
    public static SchemaModel Load(string jmxXsd) => Load([("jmx.xsd", jmxXsd)]);

    public static SchemaModel Load(params (string Name, string Content)[] files)
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            foreach (var file in files) File.WriteAllText(Path.Combine(dir.FullName, file.Name), file.Content);
            return SchemaLoader.Load(dir.FullName);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
