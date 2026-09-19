using System.Xml;

namespace JmaXml.Tests;

public class JmaXmlReaderValueTests
{
    private static JmaXmlReader AtFirstChild(XmlReader reader)
    {
        var r = new JmaXmlReader(reader);
        r.MoveToElement("a", "urn:x");
        r.Enter();
        Assert.True(r.NextChild());
        return r;
    }

    [Theory]
    [InlineData("<s>  x  </s>", "  x  ")]
    [InlineData("<s>   </s>", "   ")]
    [InlineData("<s xml:space=\"preserve\">  y </s>", "  y ")]
    [InlineData("<s/>", "")]
    [InlineData("<s>a<!-- c -->b</s>", "ab")]
    public void ReadString_returns_content_verbatim(string element, string expected)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\">{element}</a>");
        Assert.Equal(expected, AtFirstChild(reader).ReadString());
    }

    [Fact]
    public void ReadToken_collapses_xml_whitespace_only()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><t> a \t\n b　c </t></a>");
        Assert.Equal("a b　c", AtFirstChild(reader).ReadToken());
    }

    [Fact]
    public void ReadList_splits_on_xml_whitespace()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><l> x  y\nz </l></a>");
        Assert.Equal(["x", "y", "z"], AtFirstChild(reader).ReadList());
    }

    [Theory]
    [InlineData("5.9", 5.9f)]
    [InlineData("NaN", float.NaN)]
    [InlineData("-1E2", -100f)]
    public void ReadFloat_uses_XmlConvert(string text, float expected)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\"><f>{text}</f></a>");
        Assert.Equal(expected, AtFirstChild(reader).ReadFloat());
    }

    [Fact]
    public void ReadNullableFloat_returns_null_for_empty_content()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><f></f></a>");
        Assert.Null(AtFirstChild(reader).ReadNullableFloat());
    }

    [Fact]
    public void ReadInt_wraps_conversion_failures()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><i>abc</i></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).ReadInt());
        Assert.Equal("a/i", ex.Path);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Fact]
    public void ReadNullableInt_returns_null_for_empty_content()
    {
        using var emptyReader = Xml.Reader("<a xmlns=\"urn:x\"><i></i></a>");
        Assert.Null(AtFirstChild(emptyReader).ReadNullableInt());
        using var valueReader = Xml.Reader("<a xmlns=\"urn:x\"><i>42</i></a>");
        Assert.Equal(42, AtFirstChild(valueReader).ReadNullableInt());
    }

    [Fact]
    public void ReadByte_uses_XmlConvert()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><b>7</b></a>");
        Assert.Equal((byte)7, AtFirstChild(reader).ReadByte());
    }

    [Fact]
    public void ReadByte_wraps_values_outside_the_range()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><b>256</b></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).ReadByte());
        Assert.Equal("a/b", ex.Path);
        Assert.IsType<OverflowException>(ex.InnerException);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    public void ReadBool_uses_XmlConvert(string text, bool expected)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\"><b>{text}</b></a>");
        Assert.Equal(expected, AtFirstChild(reader).ReadBool());
    }

    [Fact]
    public void ReadDateTimeOffset_keeps_the_offset()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><d>2024-06-13T19:31:00+09:00</d></a>");
        Assert.Equal(new DateTimeOffset(2024, 6, 13, 19, 31, 0, TimeSpan.FromHours(9)), AtFirstChild(reader).ReadDateTimeOffset());
    }

    [Theory]
    [InlineData("\n  2024-06-13T19:31:00+09:00\n")]
    [InlineData("2024-06-13T10:31:00Z ")]
    public void ReadDateTimeOffset_accepts_surrounding_xml_whitespace(string text)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\"><d>{text}</d></a>");
        Assert.Equal(new DateTimeOffset(2024, 6, 13, 19, 31, 0, TimeSpan.FromHours(9)), AtFirstChild(reader).ReadDateTimeOffset());
    }

    [Fact]
    public void ReadDateTimeOffset_rejects_values_without_offset()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><d>2024-06-13T19:31:00</d></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).ReadDateTimeOffset());
        Assert.Equal("a/d", ex.Path);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData(" true ")]
    public void ReadNullableDateTimeOffset_returns_null_for_nil(string nil)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><d xsi:nil=\"{nil}\"/><e/></a>");
        var r = AtFirstChild(reader);
        Assert.Null(r.ReadNullableDateTimeOffset());
        Assert.True(r.NextChild());
        Assert.Equal("e", r.LocalName);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    public void ReadNullableDateTimeOffset_reads_the_value_when_nil_is_false(string nil)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><d xsi:nil=\"{nil}\">2024-06-13T19:31:00+09:00</d></a>");
        Assert.Equal(new DateTimeOffset(2024, 6, 13, 19, 31, 0, TimeSpan.FromHours(9)), AtFirstChild(reader).ReadNullableDateTimeOffset());
    }

    [Fact]
    public void Invalid_nil_value_is_rejected()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><d xsi:nil=\"yes\"/></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).ReadNullableDateTimeOffset());
        Assert.Equal("a/d/@nil", ex.Path);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Fact]
    public void ReadDuration_parses_iso8601()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><d>PT3H</d></a>");
        Assert.Equal(TimeSpan.FromHours(3), AtFirstChild(reader).ReadDuration());
    }

    [Theory]
    [InlineData("<s>a<u/>c</s>")]
    [InlineData("<s>1<u/></s>")]
    public void Child_elements_inside_simple_content_are_rejected(string element)
    {
        using var reader = Xml.Reader($"<a xmlns=\"urn:x\">{element}</a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).ReadString());
        Assert.Equal("a/s", ex.Path);
        Assert.IsType<XmlException>(ex.InnerException);
    }

    [Fact]
    public void Attributes_are_read_by_type()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><m type=\"Mj\" rank=\"2\" per=\"1.5\" ref=\"7\" flag=\"true\">4.2</m></a>");
        var r = AtFirstChild(reader);
        Assert.Equal("Mj", r.RequiredAttributeString("type"));
        Assert.Equal(2, r.RequiredAttributeInt("rank"));
        Assert.Equal(1.5f, r.AttributeFloat("per"));
        Assert.Equal((byte)7, r.AttributeByte("ref"));
        Assert.True(r.RequiredAttributeBool("flag"));
        Assert.Null(r.AttributeString("missing"));
        Assert.Equal(4.2f, r.ReadFloat());
    }

    [Fact]
    public void Missing_required_attribute_reports_the_attribute_path()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><m>4.2</m></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(reader).RequiredAttributeString("type"));
        Assert.Equal("a/m/@type", ex.Path);
    }

    [Fact]
    public void AttributeToken_collapses_whitespace()
    {
        using var reader = Xml.Reader("<a xmlns=\"urn:x\"><m t=\" a \t\n b　c \"/></a>");
        var r = AtFirstChild(reader);
        Assert.Equal("a b　c", r.AttributeToken("t"));
        Assert.Null(r.AttributeToken("missing"));
    }

    [Fact]
    public void RequiredAttributeToken_reports_the_attribute_path_when_missing()
    {
        using var missingReader = Xml.Reader("<a xmlns=\"urn:x\"><m>4.2</m></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(missingReader).RequiredAttributeToken("t"));
        Assert.Equal("a/m/@t", ex.Path);
        using var presentReader = Xml.Reader("<a xmlns=\"urn:x\"><m t=\" a \t\n b　c \"/></a>");
        Assert.Equal("a b　c", AtFirstChild(presentReader).RequiredAttributeToken("t"));
    }

    [Fact]
    public void RequiredAttributeFloat_reports_the_attribute_path_when_missing()
    {
        using var presentReader = Xml.Reader("<a xmlns=\"urn:x\"><m per=\"1.5\"/></a>");
        Assert.Equal(1.5f, AtFirstChild(presentReader).RequiredAttributeFloat("per"));
        using var missingReader = Xml.Reader("<a xmlns=\"urn:x\"><m>4.2</m></a>");
        var ex = Assert.Throws<JmaXmlException>(() => AtFirstChild(missingReader).RequiredAttributeFloat("per"));
        Assert.Equal("a/m/@per", ex.Path);
    }
}
