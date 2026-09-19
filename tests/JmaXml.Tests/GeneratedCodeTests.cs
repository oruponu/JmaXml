using JmaXml.Generator;

namespace JmaXml.Tests;

public class GeneratedCodeTests
{
    [Fact]
    public void Committed_generated_files_match_the_generator_output()
    {
        var files = CodeGenerator.Run(Fixtures.SchemaDir, TextWriter.Null);
        Assert.Empty(CodeGenerator.Diff(files, Path.Combine(Fixtures.RepoRoot, "src", "JmaXml", "Generated")));
    }

    [Fact]
    public void Schema_info_reports_the_bundled_documents()
    {
        var schema = SchemaLoader.Load(Fixtures.XsdDir);
        var dictionary = DictionaryReader.Read(Fixtures.DictionaryPath);
        Assert.Equal(dictionary.Date, SchemaInfo.DictionaryDate);
        Assert.Equal(
            schema.Versions.Select(v => (v.File, v.Namespace, v.Version, v.Date)),
            SchemaInfo.Schemas.Select(v => (v.File, v.Namespace, v.Version, v.Date)));
    }

    [Fact]
    public void Check_mode_reports_a_stale_file()
    {
        var files = CodeGenerator.Run(Fixtures.SchemaDir, TextWriter.Null);
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            CodeGenerator.Write(files, dir.FullName);
            File.WriteAllText(Path.Combine(dir.FullName, "Jmx.g.cs"), "// stale\n");
            File.WriteAllText(Path.Combine(dir.FullName, "Extra.g.cs"), "\n");
            Assert.Equal(["Jmx.g.cs: differs", "Extra.g.cs: unexpected file"], CodeGenerator.Diff(files, dir.FullName));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
