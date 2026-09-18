namespace JmaXml.Tests;

internal static class Fixtures
{
    public static string RepoRoot { get; } = FindRepoRoot();
    public static string SchemaDir => Path.Combine(RepoRoot, "schema");
    public static string XsdDir => Path.Combine(SchemaDir, "xsd");
    public static string DictionaryPath => Directory.GetFiles(SchemaDir, "jmaxml_*_dictionary.xlsx").Single();
    public static string Samples => Path.Combine(AppContext.BaseDirectory, "fixtures", "samples");
    public static string Synthetic => Path.Combine(AppContext.BaseDirectory, "fixtures", "synthetic");

    public static string Sample(string file) => File.ReadAllText(Path.Combine(Samples, file));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JmaXml.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("JmaXml.slnx not found above " + AppContext.BaseDirectory);
    }
}
