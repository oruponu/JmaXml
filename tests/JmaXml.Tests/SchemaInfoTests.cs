using JmaXml.Generator;

namespace JmaXml.Tests;

public class SchemaInfoTests
{
    [Fact]
    public void DictionaryDate_matches_the_bundled_dictionary()
    {
        Assert.Equal(DictionaryReader.Read(Fixtures.DictionaryPath).Date, SchemaInfo.DictionaryDate, StringComparer.Ordinal);
    }

    [Fact]
    public void Schemas_match_the_bundled_xsd_files()
    {
        var expected = SchemaLoader.Load(Fixtures.XsdDir).Versions.Select(v => new SchemaVersion(v.File, v.Namespace, v.Version, v.Date));
        Assert.Equal(expected, SchemaInfo.Schemas);
    }
}
