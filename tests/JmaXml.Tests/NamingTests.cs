using JmaXml.Generator;

namespace JmaXml.Tests;

public class NamingTests
{
    [Theory]
    [InlineData("type.item", "Item")]
    [InlineData("type.IntensityStation", "IntensityStation")]
    [InlineData("type.volcanoInfoContent", "VolcanoInfoContent")]
    public void TypeName_strips_the_prefix_and_capitalizes(string xsd, string expected) => Assert.Equal(expected, Naming.TypeName(xsd));

    [Theory]
    [InlineData("codeType", "CodeType")]
    [InlineData("Name", "Name")]
    [InlineData("Rank2", "Rank2")]
    public void PropertyName_capitalizes_the_first_letter(string xml, string expected) => Assert.Equal(expected, Naming.PropertyName(xml));

    [Theory]
    [InlineData("URI", "Uri")]
    [InlineData("EventDateTimeUTC", "EventDateTimeUtc")]
    [InlineData("EventID", "EventId")]
    [InlineData("refID", "RefId")]
    [InlineData("TargetDTDubious", "TargetDTDubious")]
    public void PropertyName_follows_the_dotnet_acronym_casing(string xml, string expected) => Assert.Equal(expected, Naming.PropertyName(xml));

    [Theory]
    [InlineData("Int", "@int")]
    [InlineData("Event", "@event")]
    [InlineData("Base", "@base")]
    [InlineData("Name", "name")]
    [InlineData("R", "rValue")]
    public void LocalName_escapes_keywords_and_reserved_names(string xml, string expected) => Assert.Equal(expected, Naming.LocalName(xml));

    [Fact]
    public void Official_schema_has_no_naming_collisions() => Assert.Empty(Naming.Validate(SchemaLoader.Load(Fixtures.XsdDir)));

    [Fact]
    public void Property_named_like_its_type_is_reported()
    {
        var element = new ElementModel("Area", "urn:x", false, "Area", TypeRef.Simple(Primitive.String), 1, false, false);
        var type = new TypeModel("type.Area", "urn:x", null, [], [element]);
        var schema = new SchemaModel([new NamespaceModel("jmx_seis", "urn:x", "Seismology", [type])], []);
        var errors = Naming.Validate(schema);
        Assert.Contains(errors, e => e.Contains("same as the type name", StringComparison.Ordinal));
    }

    [Fact]
    public void Content_value_colliding_with_an_attribute_is_reported()
    {
        var attribute = new AttributeModel("value", Primitive.String, true);
        var type = new TypeModel("type.X", "urn:x", Primitive.Float, [attribute], []);
        var schema = new SchemaModel([new NamespaceModel("jmx_seis", "urn:x", "Seismology", [type])], []);
        var errors = Naming.Validate(schema);
        Assert.Contains(errors, e => e.Contains("both become Value", StringComparison.Ordinal));
    }

    [Fact]
    public void Root_type_colliding_with_a_sibling_container_class_is_reported()
    {
        var type = new TypeModel("type.Seismology", "urn:x", null, [], []);
        var root = new NamespaceModel("jmx", "urn:x", null, [type]);
        var sibling = new NamespaceModel("jmx_seis", "urn:y", "Seismology", []);
        var schema = new SchemaModel([root, sibling], []);
        var errors = Naming.Validate(schema);
        Assert.Contains(errors, e => e.Contains("container class", StringComparison.Ordinal));
    }

    [Fact]
    public void TypeName_that_is_not_a_valid_identifier_is_reported()
    {
        var type = new TypeModel("type.9Value", "urn:x", null, [], []);
        var schema = new SchemaModel([new NamespaceModel("jmx_seis", "urn:x", "Seismology", [type])], []);
        var errors = Naming.Validate(schema);
        Assert.Contains(errors, e => e.Contains("not a valid C# identifier", StringComparison.Ordinal));
    }

    [Fact]
    public void PropertyName_that_is_not_a_valid_identifier_is_reported()
    {
        var element = new ElementModel("9value", "urn:x", false, "9value", TypeRef.Simple(Primitive.String), 1, false, false);
        var type = new TypeModel("type.X", "urn:x", null, [], [element]);
        var schema = new SchemaModel([new NamespaceModel("jmx_seis", "urn:x", "Seismology", [type])], []);
        var errors = Naming.Validate(schema);
        Assert.Contains(errors, e => e.Contains("not a valid C# identifier", StringComparison.Ordinal));
    }
}
