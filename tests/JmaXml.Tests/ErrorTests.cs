using System.Xml;

namespace JmaXml.Tests;

public class ErrorTests
{
    private const string Station = "<Intensity><Observation><Pref><Name>東京都</Name><Code>13</Code><Area><Name>A</Name><Code>1</Code>"
        + "<IntensityStation><Name>S</Name><Code>1</Code>{0}</IntensityStation></Area></Pref></Observation></Intensity>";

    [Theory]
    [InlineData("<Intensity><Observation><Pref><Code>13</Code></Pref></Observation></Intensity>", "Report/Body/Intensity/Observation/Pref/Name")]
    [InlineData("<Intensity><Observation><MaxInt>1</MaxInt><MaxInt>2</MaxInt></Observation></Intensity>", "Report/Body/Intensity/Observation/MaxInt[2]")]
    [InlineData("<Earthquake><ArrivalTime>2026-01-01T09:00:00+09:00</ArrivalTime></Earthquake>", "Report/Body/Earthquake/Magnitude")]
    [InlineData("<Earthquake><ArrivalTime>2026-01-01T09:00:00</ArrivalTime><jmx_eb:Magnitude type=\"Mj\">5.0</jmx_eb:Magnitude></Earthquake>", "Report/Body/Earthquake/ArrivalTime")]
    [InlineData("<Earthquake><ArrivalTime>2026-01-01T09:00:00+09:00</ArrivalTime><jmx_eb:Magnitude>5.0</jmx_eb:Magnitude></Earthquake>", "Report/Body/Earthquake/Magnitude/@type")]
    [InlineData("<Intensity><Observation><Pref><Name>東京都</Name><Code>13</Code></Pref><Pref><Code>14</Code></Pref></Observation></Intensity>", "Report/Body/Intensity/Observation/Pref[2]/Name")]
    public void Structural_violations_report_the_path(string content, string expectedPath)
    {
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(Skeleton.SeisBody(content))));
        Assert.Equal(expectedPath, ex.Path, StringComparer.Ordinal);
        Assert.True(ex.LineNumber > 0);
    }

    [Fact]
    public void Conversion_failure_wraps_FormatException()
    {
        var body = Skeleton.SeisBody(string.Format(Station, "<K>abc</K>"));
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(body)));
        Assert.Equal("Report/Body/Intensity/Observation/Pref/Area/IntensityStation/K", ex.Path, StringComparer.Ordinal);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Theory]
    [InlineData("<Text>a<b/>c</Text>", "Report/Body/Text")]
    [InlineData("<Earthquake><ArrivalTime>2026-01-01T09:00:00+09:00</ArrivalTime><jmx_eb:Magnitude type=\"Mj\">5<b/></jmx_eb:Magnitude></Earthquake>", "Report/Body/Earthquake/Magnitude")]
    public void Child_elements_inside_simple_content_are_rejected(string content, string expectedPath)
    {
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(Skeleton.SeisBody(content))));
        Assert.Equal(expectedPath, ex.Path, StringComparer.Ordinal);
        Assert.IsType<XmlException>(ex.InnerException);
    }

    [Theory]
    [InlineData("<Text>a<c</Text>")]
    [InlineData("<Text>&undefined;</Text>")]
    public void Malformed_xml_inside_simple_content_stays_an_XmlException(string content)
    {
        Assert.Throws<XmlException>(() => Report.Parse(Skeleton.Report(Skeleton.SeisBody(content))));
    }

    [Fact]
    public void Document_truncated_inside_simple_content_stays_an_XmlException()
    {
        var xml = Skeleton.Report(Skeleton.SeisBody("<Text>abc</Text>"));
        Assert.Throws<XmlException>(() => Report.Parse(xml[..(xml.IndexOf("abc", StringComparison.Ordinal) + 1)]));
    }

    [Fact]
    public void Missing_Name_in_LastKind_reports_the_element_name_not_the_type()
    {
        var head = Skeleton.Head.Replace("<Headline><Text>H</Text></Headline>",
            "<Headline><Text>H</Text><Information type=\"x\"><Item><Kind><Name>K</Name><Code>1</Code></Kind><LastKind><Code>1</Code></LastKind>"
            + "<Areas codeType=\"c\"><Area><Name>A</Name><Code>1</Code></Area></Areas></Item></Information></Headline>");
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(Skeleton.SeisBody("<Text>x</Text>"), head: head)));
        Assert.Equal("Report/Head/Headline/Information/Item/LastKind/Name", ex.Path, StringComparer.Ordinal);
    }

    [Fact]
    public void Missing_Control_and_Head()
    {
        var body = Skeleton.SeisBody("<Text>x</Text>");
        Assert.Equal("Report/Control", Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(body, control: ""))).Path, StringComparer.Ordinal);
        Assert.Equal("Report/Head", Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(body, head: ""))).Path, StringComparer.Ordinal);
    }

    [Fact]
    public void Duplicate_Body_is_rejected()
    {
        var body = Skeleton.SeisBody("<Text>1</Text>") + Skeleton.SeisBody("<Text>2</Text>");
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report(body)));
        Assert.Equal("Report/Body[2]", ex.Path, StringComparer.Ordinal);
    }

    [Fact]
    public void Missing_Body()
    {
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report("")));
        Assert.Equal("Report/Body", ex.Path, StringComparer.Ordinal);
    }

    [Fact]
    public void Unknown_Body_namespace_is_rejected_with_the_namespace_in_the_message()
    {
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse(Skeleton.Report("<Body xmlns=\"urn:x\"/>")));
        Assert.Contains("urn:x", ex.Message, StringComparison.Ordinal);
        Assert.Equal("Report/Body", ex.Path, StringComparer.Ordinal);
    }

    [Fact]
    public void Root_must_be_Report()
    {
        var ex = Assert.Throws<JmaXmlException>(() => Report.Parse("<Foo xmlns=\"http://xml.kishou.go.jp/jmaxml1/\"/>"));
        Assert.Contains("'Foo'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Dtd_is_rejected_as_XmlException()
    {
        var xml = "<!DOCTYPE Report [<!ENTITY x \"y\">]>" + Skeleton.Report(Skeleton.SeisBody("<Text>&x;</Text>")).Replace("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n", "");
        Assert.Throws<XmlException>(() => Report.Parse(xml));
    }

    [Fact]
    public void Content_after_the_root_is_rejected_as_XmlException()
    {
        Assert.Throws<XmlException>(() => Report.Parse(Skeleton.Report(Skeleton.SeisBody("<Text>x</Text>")) + "<extra/>"));
    }
}
