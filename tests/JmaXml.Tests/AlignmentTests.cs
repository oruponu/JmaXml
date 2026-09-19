using JmaXml.Generator;

namespace JmaXml.Tests;

public class AlignmentTests
{
    private static readonly SchemaModel Schema = SchemaLoader.Load(Fixtures.XsdDir);
    private static readonly DictionaryModel Dictionary = DictionaryReader.Read(Fixtures.DictionaryPath);

    private static SchemaModel SchemaWith(string prefix, TypeModel type) =>
        new([new NamespaceModel(prefix, $"urn:{prefix}", null, [type])], []);

    private static DictionaryModel DictionaryWith(string prefix, DictType type) =>
        new("2026-01-01", [new DictNamespace(prefix, [type])]);

    private static ElementModel Element(string name, int minOccurs = 1, bool unbounded = false, bool nillable = false) =>
        new(name, "urn:jmx", false, name, TypeRef.Simple(Primitive.String), minOccurs, unbounded, nillable);

    private static DictMember Member(string name, string occurrence) =>
        new(name, occurrence, "meaning", "description", []);

    [Fact]
    public void Official_schema_and_dictionary_align()
    {
        var result = Alignment.Check(Schema, Dictionary);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.All(result.Warnings, w => Assert.Contains("/*:", w, StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_dictionary_sheet_is_an_error()
    {
        var schema = SchemaWith("jmx", new TypeModel("type.X", "urn:jmx", null, [], [Element("A")]));
        var dictionary = new DictionaryModel("2026-01-01", []);
        var result = Alignment.Check(schema, dictionary);
        Assert.Equal(["jmx: sheet is missing from the dictionary"], result.Errors);
    }

    [Fact]
    public void Missing_dictionary_type_is_an_error()
    {
        var schema = SchemaWith("jmx", new TypeModel("type.X", "urn:jmx", null, [], [Element("A")]));
        var dictionary = DictionaryWith("jmx", new DictType("type.Other", "", [], []));
        var result = Alignment.Check(schema, dictionary);
        Assert.Equal(["jmx:type.X: not in the dictionary"], result.Errors);
    }

    [Fact]
    public void Missing_dictionary_element_and_attribute_are_errors()
    {
        var type = new TypeModel("type.X", "urn:jmx", null, [new AttributeModel("attr", Primitive.String, true)], [Element("A")]);
        var schema = SchemaWith("jmx", type);
        var dictionary = DictionaryWith("jmx", new DictType("type.X", "", [], []));
        var result = Alignment.Check(schema, dictionary);
        Assert.Equal(["jmx:type.X/A: not in the dictionary", "jmx:type.X/@attr: not in the dictionary"], result.Errors);
    }

    [Fact]
    public void Occurrence_mismatch_is_an_error()
    {
        var type = new TypeModel("type.X", "urn:jmx", null, [], [Element("A", minOccurs: 1)]);
        var schema = SchemaWith("jmx", type);
        var dictionary = DictionaryWith("jmx", new DictType("type.X", "", [Member("A", "?")], []));
        var result = Alignment.Check(schema, dictionary);
        Assert.Contains(result.Errors, e => e.StartsWith("jmx:type.X/A: occurrence 1 in XSD but ? in the dictionary", StringComparison.Ordinal));
    }

    [Fact]
    public void Dictionary_nil_marker_is_stripped_before_comparing_occurrence()
    {
        var type = new TypeModel("type.X", "urn:jmx", null, [], [Element("A", minOccurs: 1)]);
        var schema = SchemaWith("jmx", type);
        var dictionary = DictionaryWith("jmx", new DictType("type.X", "", [Member("A", "1(nil)")], []));
        var result = Alignment.Check(schema, dictionary);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Extra_dictionary_element_and_attribute_are_warnings()
    {
        var type = new TypeModel("type.X", "urn:jmx", null, [], []);
        var schema = SchemaWith("jmx", type);
        var dictionary = DictionaryWith("jmx", new DictType("type.X", "", [Member("*", "*")], [Member("id", "1")]));
        var result = Alignment.Check(schema, dictionary);
        Assert.Equal(["jmx:type.X/*: in the dictionary but not in the XSD", "jmx:type.X/@id: in the dictionary but not in the XSD"], result.Warnings);
    }

    [Fact]
    public void Extra_dictionary_type_is_a_warning()
    {
        var schema = SchemaWith("jmx", new TypeModel("type.X", "urn:jmx", null, [], []));
        var dictionary = new DictionaryModel("2026-01-01", [new DictNamespace("jmx", [new DictType("type.X", "", [], []), new DictType("type.Extra", "", [], [])])]);
        var result = Alignment.Check(schema, dictionary);
        Assert.Equal(["jmx:type.Extra: in the dictionary but not in the XSD"], result.Warnings);
    }
}
