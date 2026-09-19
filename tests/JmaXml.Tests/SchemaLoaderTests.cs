using JmaXml.Generator;

namespace JmaXml.Tests;

public class SchemaLoaderTests
{
    private static readonly SchemaModel Model = SchemaLoader.Load(Fixtures.XsdDir);

    private static readonly string[] OccurrenceMarks = ["1", "?", "*", "+"];

    private static string Uri(string prefix) => SchemaLoader.KnownNamespaces.First(n => n.Prefix == prefix).Uri;

    [Fact]
    public void Six_namespaces_with_class_names_and_at_least_one_type_each()
    {
        Assert.Equal(["jmx", "jmx_ib", "jmx_eb", "jmx_mete", "jmx_seis", "jmx_volc"], Model.Namespaces.Select(n => n.Prefix));
        Assert.Equal([null, "InformationBasis", "ElementBasis", "Meteorology", "Seismology", "Volcanology"], Model.Namespaces.Select(n => n.ClassName));
        Assert.All(Model.Namespaces, n => Assert.NotEmpty(n.Types));
    }

    [Fact]
    public void Every_schema_file_has_a_version_and_a_date()
    {
        Assert.NotEmpty(Model.Versions);
        Assert.All(Model.Versions, v => Assert.NotEmpty(v.Version));
        Assert.All(Model.Versions, v => Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", v.Date));
    }

    [Fact]
    public void Every_type_and_element_in_the_official_model_is_internally_consistent()
    {
        var types = Model.Namespaces.SelectMany(n => n.Types).ToList();
        Assert.All(types, t => Assert.True(t.Content is null || t.Elements.Length == 0));
        var elements = types.SelectMany(t => t.Elements).ToList();
        Assert.NotEmpty(elements);
        Assert.All(elements, e => Assert.Contains(e.Occurrence, OccurrenceMarks));
        Assert.All(elements, e => Assert.True(
            e.Type.Primitive is not null
            || (e.Type.IsComplex && Model.Type(e.Type.Namespace!, e.Type.TypeName!) is not null)));
    }

    [Fact]
    public void Element_order_is_preserved_and_occurrence_maps_from_minOccurs_and_maxOccurs()
    {
        var jmx = Uri("jmx");
        var model = TestXsd.Load($"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="{jmx}" elementFormDefault="qualified">
              <xs:complexType name="type.Order">
                <xs:sequence>
                  <xs:element name="A" type="xs:string"/>
                  <xs:element name="B" type="xs:string" minOccurs="0"/>
                  <xs:element name="C" type="xs:string" minOccurs="0" maxOccurs="unbounded"/>
                  <xs:element name="D" type="xs:string" maxOccurs="unbounded"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """);
        var type = model.Type(jmx, "type.Order")!;
        Assert.Equal(["A", "B", "C", "D"], type.Elements.Select(e => e.Name));
        Assert.Equal(["1", "?", "*", "+"], type.Elements.Select(e => e.Occurrence));
    }

    [Fact]
    public void Complex_element_type_keeps_the_referenced_types_namespace_and_name()
    {
        var jmx = Uri("jmx");
        var model = TestXsd.Load($"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:jmx="{jmx}" targetNamespace="{jmx}" elementFormDefault="qualified">
              <xs:complexType name="type.Inner">
                <xs:sequence>
                  <xs:element name="X" type="xs:string"/>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="type.Outer">
                <xs:sequence>
                  <xs:element name="Child" type="jmx:type.Inner"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """);
        var child = model.Type(jmx, "type.Outer")!.Elements.Single(e => e.Name == "Child");
        Assert.True(child.Type.IsComplex);
        Assert.Equal("type.Inner", child.Type.TypeName);
        Assert.Equal(jmx, child.Type.Namespace);
    }

    [Fact]
    public void Referenced_element_into_another_namespace_keeps_its_name_namespace_dictionary_key_and_occurrence()
    {
        var jmx = Uri("jmx");
        var jmxEb = Uri("jmx_eb");
        var model = TestXsd.Load(
            ("jmx.xsd", $"""
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:jmx_eb="{jmxEb}" targetNamespace="{jmx}" elementFormDefault="qualified">
                  <xs:import namespace="{jmxEb}" schemaLocation="eb.xsd"/>
                  <xs:complexType name="type.Holder">
                    <xs:sequence>
                      <xs:element ref="jmx_eb:Thing" maxOccurs="unbounded"/>
                    </xs:sequence>
                  </xs:complexType>
                </xs:schema>
                """),
            ("eb.xsd", $"""
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="{jmxEb}" elementFormDefault="qualified">
                  <xs:element name="Thing" type="xs:string"/>
                </xs:schema>
                """));
        var thing = model.Type(jmx, "type.Holder")!.Elements.Single(e => e.Name == "Thing");
        Assert.True(thing.IsRef);
        Assert.Equal(jmxEb, thing.Namespace);
        Assert.Equal("jmx_eb:Thing", thing.DictKey);
        Assert.Equal("+", thing.Occurrence);
    }

    [Fact]
    public void Simple_content_type_exposes_its_value_primitive_and_distinguishes_required_attributes()
    {
        var jmx = Uri("jmx");
        var model = TestXsd.Load($"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="{jmx}" elementFormDefault="qualified">
              <xs:complexType name="type.Value">
                <xs:simpleContent>
                  <xs:extension base="xs:float">
                    <xs:attribute name="type" type="xs:string" use="required"/>
                    <xs:attribute name="unit" type="xs:string" use="optional"/>
                  </xs:extension>
                </xs:simpleContent>
              </xs:complexType>
            </xs:schema>
            """);
        var type = model.Type(jmx, "type.Value")!;
        Assert.Equal(Primitive.Float, type.Content);
        Assert.Equal([("type", true), ("unit", false)], type.Attributes.Select(a => (a.Name, a.Required)));
    }

    [Fact]
    public void Nillable_element_and_union_and_list_primitives_are_mapped()
    {
        var jmx = Uri("jmx");
        var model = TestXsd.Load($"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:jmx="{jmx}" targetNamespace="{jmx}" elementFormDefault="qualified">
              <xs:simpleType name="NullableFloat">
                <xs:union>
                  <xs:simpleType><xs:restriction base="xs:float"/></xs:simpleType>
                  <xs:simpleType><xs:restriction base="xs:string"><xs:enumeration value=""/></xs:restriction></xs:simpleType>
                </xs:union>
              </xs:simpleType>
              <xs:simpleType name="StringList">
                <xs:list itemType="xs:string"/>
              </xs:simpleType>
              <xs:complexType name="type.Mixed">
                <xs:sequence>
                  <xs:element name="When" type="xs:dateTime" nillable="true"/>
                  <xs:element name="Maybe" type="jmx:NullableFloat"/>
                  <xs:element name="Many" type="jmx:StringList"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """);
        var type = model.Type(jmx, "type.Mixed")!;
        var when = type.Elements.Single(e => e.Name == "When");
        Assert.True(when.Nillable);
        Assert.Equal(Primitive.DateTimeOffset, when.Type.Primitive);
        Assert.Equal(Primitive.NullableFloat, type.Elements.Single(e => e.Name == "Maybe").Type.Primitive);
        Assert.Equal(Primitive.StringList, type.Elements.Single(e => e.Name == "Many").Type.Primitive);
    }

    [Fact]
    public void Built_in_primitive_types_are_mapped_to_the_expected_primitive()
    {
        var jmx = Uri("jmx");
        var model = TestXsd.Load($"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="{jmx}" elementFormDefault="qualified">
              <xs:complexType name="type.Primitives">
                <xs:sequence>
                  <xs:element name="S" type="xs:string"/>
                  <xs:element name="T" type="xs:token"/>
                  <xs:element name="F" type="xs:float"/>
                  <xs:element name="I" type="xs:int"/>
                  <xs:element name="By" type="xs:unsignedByte"/>
                  <xs:element name="Bo" type="xs:boolean"/>
                  <xs:element name="Dt" type="xs:dateTime"/>
                  <xs:element name="Du" type="xs:duration"/>
                </xs:sequence>
              </xs:complexType>
            </xs:schema>
            """);
        var type = model.Type(jmx, "type.Primitives")!;
        Assert.Equal(Primitive.String, type.Elements.Single(e => e.Name == "S").Type.Primitive);
        Assert.Equal(Primitive.Token, type.Elements.Single(e => e.Name == "T").Type.Primitive);
        Assert.Equal(Primitive.Float, type.Elements.Single(e => e.Name == "F").Type.Primitive);
        Assert.Equal(Primitive.Int, type.Elements.Single(e => e.Name == "I").Type.Primitive);
        Assert.Equal(Primitive.Byte, type.Elements.Single(e => e.Name == "By").Type.Primitive);
        Assert.Equal(Primitive.Bool, type.Elements.Single(e => e.Name == "Bo").Type.Primitive);
        Assert.Equal(Primitive.DateTimeOffset, type.Elements.Single(e => e.Name == "Dt").Type.Primitive);
        Assert.Equal(Primitive.Duration, type.Elements.Single(e => e.Name == "Du").Type.Primitive);
    }

    [Fact]
    public void Unsupported_constructs_fail_loudly()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "jmx.xsd"), """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="http://xml.kishou.go.jp/jmaxml1/" elementFormDefault="qualified">
                  <xs:complexType name="type.x"><xs:choice><xs:element name="A" type="xs:string"/></xs:choice></xs:complexType>
                </xs:schema>
                """);
            var ex = Assert.Throws<SchemaException>(() => SchemaLoader.Load(dir.FullName));
            Assert.Contains("XmlSchemaChoice", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
