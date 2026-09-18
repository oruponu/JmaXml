namespace JmaXml.Tests;

public class FixturesTests
{
    [Fact]
    public void Official_samples_are_copied_to_output()
    {
        Assert.Equal(546, Directory.GetFiles(Fixtures.Samples, "*.xml").Length);
    }

    [Fact]
    public void Schema_inputs_exist()
    {
        Assert.Equal(8, Directory.GetFiles(Fixtures.XsdDir, "*.xsd").Length);
        Assert.EndsWith("jmaxml_20260129_dictionary.xlsx", Fixtures.DictionaryPath, StringComparison.Ordinal);
    }
}
